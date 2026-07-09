using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Clinic.Saas.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IAuditService _audit;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(IDbConnectionFactory connectionFactory, IAuditService audit, ILogger<SubscriptionService> logger)
    {
        _connectionFactory = connectionFactory;
        _audit = audit;
        _logger = logger;
    }

    public async Task<TenantSubscriptionDto> CreateInitialSubscriptionAsync(Guid tenantId, Guid? planId, Guid? createdByUserId = null)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        var plan = await ResolvePlanAsync(connection, planId);
        var status = plan.Code.Equals("TRIAL", StringComparison.OrdinalIgnoreCase) ? SubscriptionStatus.Trial : SubscriptionStatus.Active;
        var now = DateTime.UtcNow;

        const string sql = @"
DECLARE @Id UNIQUEIDENTIFIER = NEWID();
DECLARE @EndsAt DATETIME2 = DATEADD(day, @DurationDays, @Now);

UPDATE dbo.TenantSubscriptions
SET Status = @Cancelled, CancelledAtUtc = @Now, UpdatedAtUtc = @Now
WHERE TenantId = @TenantId AND Status IN (@Trial, @Active, @PastDue);

INSERT INTO dbo.TenantSubscriptions
(Id, TenantId, PlanId, Status, StartsAtUtc, EndsAtUtc, AutoRenew, GracePeriodDays, Notes, CreatedByUserId, CreatedAtUtc)
VALUES
(@Id, @TenantId, @PlanId, @Status, @Now, @EndsAt, 0, @GracePeriodDays, @Notes, @CreatedByUserId, @Now);

UPDATE dbo.Tenants
SET IsActive = 1,
    SubscriptionState = @State,
    TrialEndsAt = CASE WHEN @Status = @Trial THEN @EndsAt ELSE TrialEndsAt END,
    UpdatedAt = @Now
WHERE Id = @TenantId;";

        await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            PlanId = plan.Id,
            Status = status,
            Trial = SubscriptionStatus.Trial,
            Active = SubscriptionStatus.Active,
            PastDue = SubscriptionStatus.PastDue,
            Cancelled = SubscriptionStatus.Cancelled,
            State = status == SubscriptionStatus.Trial ? "Trial" : "Active",
            DurationDays = plan.DurationDays,
            GracePeriodDays = await GetDefaultGracePeriodDaysAsync(connection),
            Notes = "Initial subscription",
            CreatedByUserId = createdByUserId,
            Now = now
        });

        await LogAsync("CreateInitialSubscription", tenantId, tenantId, createdByUserId, new { plan.Id, plan.Code, status });
        return await GetCurrentSubscriptionAsync(tenantId) ?? throw new InvalidOperationException("Initial subscription was not created.");
    }

    public async Task<TenantSubscriptionDto?> GetCurrentSubscriptionAsync(Guid tenantId)
    {
        const string sql = CurrentSubscriptionSql + " WHERE s.TenantId = @TenantId ORDER BY s.EndsAtUtc DESC, s.CreatedAtUtc DESC;";
        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<TenantSubscriptionDto>(sql, new { TenantId = tenantId });
    }

    public async Task<SubscriptionStatusDto> GetSubscriptionStatusAsync(Guid tenantId)
    {
        const string sql = @"
SELECT TOP (1)
    t.Id AS TenantId,
    s.Status AS SubscriptionStatus,
    s.EndsAtUtc AS SubscriptionEndsAtUtc,
    CASE WHEN s.EndsAtUtc < SYSUTCDATETIME() AND SYSUTCDATETIME() <= DATEADD(day, s.GracePeriodDays, s.EndsAtUtc) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsInGracePeriod,
    CASE WHEN s.EndsAtUtc IS NULL THEN 0 ELSE DATEDIFF(day, SYSUTCDATETIME(), s.EndsAtUtc) END AS DaysRemaining,
    COALESCE(t.SubscriptionState, CASE WHEN t.IsActive = 1 THEN N'Active' ELSE N'Disabled' END) AS TenantStatus,
    t.IsActive
FROM dbo.Tenants t
OUTER APPLY (
    SELECT TOP (1) *
    FROM dbo.TenantSubscriptions latest
    WHERE latest.TenantId = t.Id
    ORDER BY latest.EndsAtUtc DESC, latest.CreatedAtUtc DESC
) s
WHERE t.Id = @TenantId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<SubscriptionStatusDto>(sql, new { TenantId = tenantId })
               ?? new SubscriptionStatusDto(tenantId, null, null, false, 0, "Disabled", false);
    }

    public async Task<TenantSubscriptionDto?> RenewSubscriptionAsync(RenewTenantSubscriptionRequest request, Guid? renewedByUserId)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        var tenantExists = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.Tenants WHERE Id = @TenantId;", new { request.TenantId });
        if (tenantExists == 0)
        {
            return null;
        }

        var plan = await ResolvePlanAsync(connection, request.PlanId);
        var current = await GetCurrentSubscriptionAsync(request.TenantId);
        var now = DateTime.UtcNow;
        var startsAt = current is not null && current.EndsAtUtc > now ? current.EndsAtUtc : now;
        var endsAt = request.CustomEndDateUtc?.ToUniversalTime() ?? startsAt.AddDays(plan.DurationDays);
        var paidAt = request.PaymentDateUtc?.ToUniversalTime() ?? now;
        var actualPaidAmount = ResolvePaidAmount(request.ActualPaidAmount, plan.Price);

        const string sql = @"
DECLARE @SubscriptionId UNIQUEIDENTIFIER = NEWID();

UPDATE dbo.TenantSubscriptions
SET Status = @Cancelled, CancelledAtUtc = @Now, UpdatedAtUtc = @Now
WHERE TenantId = @TenantId AND Status IN (@Trial, @Active, @PastDue, @Expired, @Suspended);

INSERT INTO dbo.TenantSubscriptions
(Id, TenantId, PlanId, Status, StartsAtUtc, EndsAtUtc, RenewedAtUtc, AutoRenew, GracePeriodDays, Notes, CreatedByUserId, CreatedAtUtc)
VALUES
(@SubscriptionId, @TenantId, @PlanId, @Active, @StartsAt, @EndsAt, @Now, 0, @GracePeriodDays, @Notes, @RenewedByUserId, @Now);

INSERT INTO dbo.SubscriptionPayments
(Id, TenantId, SubscriptionId, Amount, Currency, PaymentStatus, PaymentMethod, PaidAtUtc, CreatedAtUtc, Notes, CreatedByUserId)
VALUES
(NEWID(), @TenantId, @SubscriptionId, @ActualPaidAmount, @Currency, @Paid, @PaymentMethod, @PaidAt, @Now, @Notes, @RenewedByUserId);

UPDATE dbo.Tenants
SET IsActive = 1,
    SubscriptionState = N'Active',
    [Plan] = CASE
        WHEN @PlanCode = N'BASIC_MONTHLY' THEN 1
        WHEN @PlanCode = N'PRO_MONTHLY' THEN 2
        WHEN @PlanCode = N'ANNUAL' THEN 3
        ELSE [Plan]
    END,
    UpdatedAt = @Now
WHERE Id = @TenantId;

SELECT @SubscriptionId;";

        var subscriptionId = await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            request.TenantId,
            PlanId = plan.Id,
            PlanCode = plan.Code,
            plan.Currency,
            ActualPaidAmount = actualPaidAmount,
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? null : request.PaymentMethod.Trim(),
            PaidAt = paidAt,
            Paid = SubscriptionPaymentStatus.Paid,
            Active = SubscriptionStatus.Active,
            Trial = SubscriptionStatus.Trial,
            PastDue = SubscriptionStatus.PastDue,
            Expired = SubscriptionStatus.Expired,
            Suspended = SubscriptionStatus.Suspended,
            Cancelled = SubscriptionStatus.Cancelled,
            StartsAt = startsAt,
            EndsAt = endsAt,
            GracePeriodDays = await GetDefaultGracePeriodDaysAsync(connection),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            RenewedByUserId = renewedByUserId,
            Now = now
        });

        await LogAsync("RenewSubscription", request.TenantId, subscriptionId, renewedByUserId, new
        {
            plan.Id,
            plan.Code,
            startsAt,
            endsAt,
            amount = actualPaidAmount,
            paidAt,
            request.PaymentMethod,
            request.Notes
        });
        return await GetCurrentSubscriptionAsync(request.TenantId);
    }

    public async Task<bool> SuspendTenantAsync(Guid tenantId, string reason, Guid? suspendedByUserId)
    {
        const string sql = @"
UPDATE dbo.Tenants
SET IsActive = 0, SubscriptionState = N'Suspended', UpdatedAt = SYSUTCDATETIME()
WHERE Id = @TenantId;

UPDATE dbo.TenantSubscriptions
SET Status = @Suspended, SuspendedAtUtc = SYSUTCDATETIME(), Notes = COALESCE(@Reason, Notes), UpdatedAtUtc = SYSUTCDATETIME()
WHERE Id = (
    SELECT TOP (1) Id FROM dbo.TenantSubscriptions
    WHERE TenantId = @TenantId AND Status IN (@Trial, @Active, @PastDue)
    ORDER BY EndsAtUtc DESC, CreatedAtUtc DESC
);";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        var rows = await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            Reason = reason,
            Trial = SubscriptionStatus.Trial,
            Active = SubscriptionStatus.Active,
            PastDue = SubscriptionStatus.PastDue,
            Suspended = SubscriptionStatus.Suspended
        });

        await LogAsync("SuspendTenant", tenantId, tenantId, suspendedByUserId, new { reason });
        return rows > 0;
    }

    public async Task<bool> ReactivateTenantAsync(Guid tenantId, Guid? reactivatedByUserId)
    {
        var current = await GetCurrentSubscriptionAsync(tenantId);
        if (current is null || current.EndsAtUtc <= DateTime.UtcNow || current.Status is SubscriptionStatus.Expired or SubscriptionStatus.Cancelled)
        {
            return false;
        }

        const string sql = @"
UPDATE dbo.Tenants
SET IsActive = 1, SubscriptionState = N'Active', UpdatedAt = SYSUTCDATETIME()
WHERE Id = @TenantId;

UPDATE dbo.TenantSubscriptions
SET Status = @Active, SuspendedAtUtc = NULL, UpdatedAtUtc = SYSUTCDATETIME()
WHERE Id = @SubscriptionId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        await connection.ExecuteAsync(sql, new { TenantId = tenantId, SubscriptionId = current.Id, Active = SubscriptionStatus.Active });
        await LogAsync("ReactivateTenant", tenantId, tenantId, reactivatedByUserId, null);
        return true;
    }

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string? reason)
    {
        const string sql = @"
UPDATE dbo.TenantSubscriptions
SET Status = @Cancelled, CancelledAtUtc = SYSUTCDATETIME(), Notes = COALESCE(@Reason, Notes), UpdatedAtUtc = SYSUTCDATETIME()
WHERE Id = @SubscriptionId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        var rows = await connection.ExecuteAsync(sql, new { SubscriptionId = subscriptionId, Cancelled = SubscriptionStatus.Cancelled, Reason = reason });
        return rows > 0;
    }

    public async Task<SubscriptionExpiryResultDto> CheckAndExpireSubscriptionsAsync()
    {
        const string sql = @"
SELECT s.Id, s.TenantId, s.EndsAtUtc, s.GracePeriodDays, s.Status
FROM dbo.TenantSubscriptions s
WHERE s.Status IN (@Trial, @Active, @PastDue);";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        var rows = (await connection.QueryAsync<ExpiryRow>(sql, new
        {
            Trial = SubscriptionStatus.Trial,
            Active = SubscriptionStatus.Active,
            PastDue = SubscriptionStatus.PastDue
        })).ToList();

        var result = new MutableExpiryResult { Checked = rows.Count };
        var now = DateTime.UtcNow;

        foreach (var row in rows)
        {
            try
            {
                if (row.EndsAtUtc >= now)
                {
                    await TouchCheckedAsync(connection, row.Id, now);
                    result.Skipped++;
                    continue;
                }

                var inGrace = row.GracePeriodDays > 0 && now <= row.EndsAtUtc.AddDays(row.GracePeriodDays);
                if (inGrace)
                {
                    await MarkPastDueAsync(connection, row.Id, row.TenantId, now);
                    result.MarkedPastDue++;
                }
                else
                {
                    await MarkExpiredAsync(connection, row.Id, row.TenantId, now);
                    result.MarkedExpired++;
                }
            }
            catch (Exception ex)
            {
                result.Errors++;
                _logger.LogError(ex, "Failed checking subscription {SubscriptionId}", row.Id);
            }
        }

        _logger.LogInformation("Subscription expiry check completed. Checked={Checked} PastDue={PastDue} Expired={Expired} Skipped={Skipped} Errors={Errors}",
            result.Checked, result.MarkedPastDue, result.MarkedExpired, result.Skipped, result.Errors);

        return new SubscriptionExpiryResultDto(result.Checked, result.MarkedPastDue, result.MarkedExpired, result.Skipped, result.Errors);
    }

    public async Task<IReadOnlyList<TenantSubscriptionDto>> GetExpiringSoonSubscriptionsAsync(int days)
    {
        const string sql = CurrentSubscriptionSql + @"
WHERE s.Status IN (@Trial, @Active, @PastDue)
  AND s.EndsAtUtc >= SYSUTCDATETIME()
  AND s.EndsAtUtc <= DATEADD(day, @Days, SYSUTCDATETIME())
ORDER BY s.EndsAtUtc;";
        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return (await connection.QueryAsync<TenantSubscriptionDto>(sql, new { Days = days, Trial = SubscriptionStatus.Trial, Active = SubscriptionStatus.Active, PastDue = SubscriptionStatus.PastDue })).ToList();
    }

    public async Task<IReadOnlyList<TenantSubscriptionDto>> GetExpiredSubscriptionsAsync()
    {
        const string sql = CurrentSubscriptionSql + @" WHERE s.Status = @Expired ORDER BY s.EndsAtUtc DESC;";
        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return (await connection.QueryAsync<TenantSubscriptionDto>(sql, new { Expired = SubscriptionStatus.Expired })).ToList();
    }

    public async Task<IReadOnlyList<TenantSubscriptionDto>> GetSubscriptionsAsync(PlatformSubscriptionFilterDto filter)
    {
        const string sql = CurrentSubscriptionSql + @"
WHERE (@TenantId IS NULL OR s.TenantId = @TenantId)
  AND (@Status IS NULL OR s.Status = @Status)
  AND (@PlanId IS NULL OR s.PlanId = @PlanId)
  AND (@ExpiresBefore IS NULL OR s.EndsAtUtc <= @ExpiresBefore)
  AND (@ExpiresAfter IS NULL OR s.EndsAtUtc >= @ExpiresAfter)
ORDER BY s.EndsAtUtc DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);
        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return (await connection.QueryAsync<TenantSubscriptionDto>(sql, new
        {
            filter.TenantId,
            filter.Status,
            filter.PlanId,
            filter.ExpiresBefore,
            filter.ExpiresAfter,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        })).ToList();
    }

    private static async Task TouchCheckedAsync(System.Data.IDbConnection connection, Guid id, DateTime now)
    {
        await connection.ExecuteAsync("UPDATE dbo.TenantSubscriptions SET LastCheckedAtUtc = @Now WHERE Id = @Id;", new { Id = id, Now = now });
    }

    private async Task MarkPastDueAsync(System.Data.IDbConnection connection, Guid id, Guid tenantId, DateTime now)
    {
        const string sql = @"
UPDATE dbo.TenantSubscriptions
SET Status = @PastDue, LastCheckedAtUtc = @Now, UpdatedAtUtc = @Now
WHERE Id = @Id AND Status <> @PastDue;

UPDATE dbo.Tenants
SET IsActive = 1, SubscriptionState = N'PastDue', UpdatedAt = @Now
WHERE Id = @TenantId AND SubscriptionState <> N'PastDue';";
        await connection.ExecuteAsync(sql, new { Id = id, TenantId = tenantId, PastDue = SubscriptionStatus.PastDue, Now = now });
        await LogAsync("SubscriptionPastDue", tenantId, id, null, null);
    }

    private async Task MarkExpiredAsync(System.Data.IDbConnection connection, Guid id, Guid tenantId, DateTime now)
    {
        const string sql = @"
UPDATE dbo.TenantSubscriptions
SET Status = @Expired, LastCheckedAtUtc = @Now, UpdatedAtUtc = @Now
WHERE Id = @Id AND Status <> @Expired
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.TenantSubscriptions newer
      WHERE newer.TenantId = @TenantId
        AND newer.Id <> @Id
        AND newer.Status IN (@Trial, @Active, @PastDue)
        AND newer.EndsAtUtc > @Now
  );

IF @@ROWCOUNT > 0
BEGIN
    UPDATE dbo.Tenants
    SET IsActive = 0, SubscriptionState = N'Expired', UpdatedAt = @Now
    WHERE Id = @TenantId;
END";
        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            TenantId = tenantId,
            Expired = SubscriptionStatus.Expired,
            Trial = SubscriptionStatus.Trial,
            Active = SubscriptionStatus.Active,
            PastDue = SubscriptionStatus.PastDue,
            Now = now
        });
        await LogAsync("SubscriptionExpired", tenantId, id, null, null);
    }

    private static async Task<int> GetDefaultGracePeriodDaysAsync(System.Data.IDbConnection connection)
    {
        const string sql = @"SELECT TRY_CAST([Value] AS int) FROM dbo.PlatformSettings WHERE [Key] = N'DefaultGracePeriodDays';";
        return await connection.ExecuteScalarAsync<int?>(sql) ?? 0;
    }

    private static async Task<PlanRow> ResolvePlanAsync(System.Data.IDbConnection connection, Guid? planId)
    {
        const string sql = @"
SELECT TOP (1) Id, Code, DurationDays, Currency, Price
FROM dbo.SubscriptionPlans
WHERE (@PlanId IS NOT NULL AND Id = @PlanId)
   OR (@PlanId IS NULL AND Code = N'TRIAL')
ORDER BY CASE WHEN @PlanId IS NOT NULL AND Id = @PlanId THEN 0 ELSE 1 END;";
        return await connection.QueryFirstOrDefaultAsync<PlanRow>(sql, new { PlanId = planId })
               ?? throw new InvalidOperationException("Subscription plan was not found.");
    }

    private static decimal ResolvePaidAmount(decimal? requestedAmount, decimal planPrice)
    {
        if (requestedAmount.HasValue && requestedAmount.Value > 0)
        {
            return requestedAmount.Value;
        }

        return planPrice > 0 ? planPrice : 0;
    }

    private Task LogAsync(string action, Guid tenantId, Guid entityId, Guid? userId, object? values)
    {
        return _audit.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            EntityName = "TenantSubscription",
            EntityId = entityId,
            NewValues = values is null ? null : System.Text.Json.JsonSerializer.Serialize(values),
            CreatedAt = DateTime.UtcNow
        });
    }

    private const string CurrentSubscriptionSql = @"
SELECT
    s.Id,
    s.TenantId,
    s.PlanId,
    p.Name AS PlanName,
    p.Code AS PlanCode,
    s.Status,
    s.StartsAtUtc,
    s.EndsAtUtc,
    s.RenewedAtUtc,
    s.CancelledAtUtc,
    s.SuspendedAtUtc,
    s.AutoRenew,
    s.GracePeriodDays,
    s.LastCheckedAtUtc,
    s.Notes,
    DATEDIFF(day, SYSUTCDATETIME(), s.EndsAtUtc) AS DaysRemaining,
    CASE WHEN s.EndsAtUtc < SYSUTCDATETIME() AND SYSUTCDATETIME() <= DATEADD(day, s.GracePeriodDays, s.EndsAtUtc) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsInGracePeriod,
    COALESCE(pay.TotalPaidAmount, 0) AS ActualPaidAmount,
    pay.PaidAtUtc AS PaymentDateUtc,
    latestPayment.PaymentMethod
FROM dbo.TenantSubscriptions s
JOIN dbo.SubscriptionPlans p ON p.Id = s.PlanId
OUTER APPLY (
    SELECT SUM(Amount) AS TotalPaidAmount, MAX(PaidAtUtc) AS PaidAtUtc
    FROM dbo.SubscriptionPayments subscriptionPayment
    WHERE subscriptionPayment.SubscriptionId = s.Id
      AND subscriptionPayment.PaymentStatus = 2
) pay
OUTER APPLY (
    SELECT TOP (1) PaymentMethod
    FROM dbo.SubscriptionPayments subscriptionPayment
    WHERE subscriptionPayment.SubscriptionId = s.Id
      AND subscriptionPayment.PaymentStatus = 2
    ORDER BY subscriptionPayment.PaidAtUtc DESC, subscriptionPayment.CreatedAtUtc DESC
) latestPayment";

    private sealed class PlanRow
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int DurationDays { get; set; }
        public string Currency { get; set; } = "EGP";
        public decimal Price { get; set; }
    }

    private sealed class ExpiryRow
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public DateTime EndsAtUtc { get; set; }
        public int GracePeriodDays { get; set; }
        public SubscriptionStatus Status { get; set; }
    }

    private sealed class MutableExpiryResult
    {
        public int Checked { get; set; }
        public int MarkedPastDue { get; set; }
        public int MarkedExpired { get; set; }
        public int Skipped { get; set; }
        public int Errors { get; set; }
    }
}
