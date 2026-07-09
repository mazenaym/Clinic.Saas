using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Dapper;

namespace Clinic.Saas.Infrastructure.Services;

public class PlatformDashboardService : IPlatformDashboardService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISubscriptionService _subscriptions;
    private readonly IAuditService _audit;

    public PlatformDashboardService(IDbConnectionFactory connectionFactory, ISubscriptionService subscriptions, IAuditService audit)
    {
        _connectionFactory = connectionFactory;
        _subscriptions = subscriptions;
        _audit = audit;
    }

    public async Task<PlatformDashboardSummaryDto> GetDashboardSummaryAsync()
    {
        const string sql = @"
SELECT
    COUNT(1) AS TotalClinics,
    SUM(CASE WHEN t.IsActive = 1 AND COALESCE(t.SubscriptionState, N'Active') = N'Active' THEN 1 ELSE 0 END) AS ActiveClinics,
    SUM(CASE WHEN COALESCE(t.SubscriptionState, N'') = N'Trial' THEN 1 ELSE 0 END) AS TrialClinics,
    SUM(CASE WHEN COALESCE(t.SubscriptionState, N'') = N'Expired' THEN 1 ELSE 0 END) AS ExpiredClinics,
    SUM(CASE WHEN COALESCE(t.SubscriptionState, N'') = N'Suspended' THEN 1 ELSE 0 END) AS SuspendedClinics
FROM dbo.Tenants t
WHERE t.Subdomain <> N'platform';

SELECT COUNT(1) FROM dbo.Users;
SELECT COUNT(1) FROM dbo.Patients WHERE IsDeleted = 0;
SELECT COUNT(1) FROM dbo.Appointments WHERE IsDeleted = 0;

SELECT COALESCE(SUM(Amount), 0)
FROM dbo.SubscriptionPayments
WHERE PaymentStatus = @Paid
  AND PaidAtUtc >= DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1)
  AND PaidAtUtc < DATEADD(month, 1, DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1));

SELECT COALESCE(SUM(Amount), 0)
FROM dbo.SubscriptionPayments
WHERE PaymentStatus = @Paid
  AND PaidAtUtc >= DATEFROMPARTS(YEAR(SYSUTCDATETIME()), 1, 1)
  AND PaidAtUtc < DATEADD(year, 1, DATEFROMPARTS(YEAR(SYSUTCDATETIME()), 1, 1));

SELECT COUNT(1)
FROM dbo.TenantSubscriptions
WHERE Status IN (@Trial, @Active, @PastDue)
  AND EndsAtUtc >= SYSUTCDATETIME()
  AND EndsAtUtc <= DATEADD(day, 7, SYSUTCDATETIME());

SELECT COUNT(1)
FROM dbo.TenantSubscriptions
WHERE Status = @Expired;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            Paid = SubscriptionPaymentStatus.Paid,
            Trial = SubscriptionStatus.Trial,
            Active = SubscriptionStatus.Active,
            PastDue = SubscriptionStatus.PastDue,
            Expired = SubscriptionStatus.Expired
        });

        var counts = await multi.ReadSingleAsync<DashboardCounts>();
        var totalUsers = await multi.ReadSingleAsync<int>();
        var totalPatients = await multi.ReadSingleAsync<int>();
        var totalAppointments = await multi.ReadSingleAsync<int>();
        var monthlyRevenue = await multi.ReadSingleAsync<decimal>();
        var annualRevenue = await multi.ReadSingleAsync<decimal>();
        var expiringSoonCount = await multi.ReadSingleAsync<int>();
        var expiredCount = await multi.ReadSingleAsync<int>();

        return new PlatformDashboardSummaryDto(
            counts.TotalClinics,
            counts.ActiveClinics,
            counts.TrialClinics,
            counts.ExpiredClinics,
            counts.SuspendedClinics,
            totalUsers,
            totalPatients,
            totalAppointments,
            monthlyRevenue,
            annualRevenue,
            expiringSoonCount,
            expiredCount,
            await GetRecentClinicsAsync(),
            await _subscriptions.GetExpiringSoonSubscriptionsAsync(7));
    }

    public async Task<IReadOnlyList<AdminClinicDto>> GetClinicsOverviewAsync(PlatformClinicFilterDto filter)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);
        const string sql = ClinicSelect + @"
WHERE (@Search IS NULL OR t.Name LIKE N'%' + @Search + N'%' OR t.Subdomain LIKE N'%' + @Search + N'%' OR t.Email LIKE N'%' + @Search + N'%')
  AND (@Status IS NULL OR COALESCE(t.SubscriptionState, CASE WHEN t.IsActive = 1 THEN N'Active' ELSE N'Disabled' END) = @Status)
  AND (@PlanId IS NULL OR ts.PlanId = @PlanId)
  AND (@SubscriptionStatus IS NULL OR ts.Status = @SubscriptionStatus)
  AND (@ExpiresBefore IS NULL OR ts.EndsAtUtc <= @ExpiresBefore)
  AND (@ExpiresAfter IS NULL OR ts.EndsAtUtc >= @ExpiresAfter)
ORDER BY t.CreatedAt DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return (await connection.QueryAsync<AdminClinicDto>(sql, new
        {
            filter.Search,
            filter.Status,
            filter.PlanId,
            filter.SubscriptionStatus,
            filter.ExpiresBefore,
            filter.ExpiresAfter,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        })).ToList();
    }

    public async Task<IReadOnlyList<AdminClinicDto>> GetRecentClinicsAsync(int take = 5)
    {
        var sql = ClinicSelect + " ORDER BY t.CreatedAt DESC OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY;";
        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return (await connection.QueryAsync<AdminClinicDto>(sql, new { Take = take })).ToList();
    }

    public async Task<PlatformReportsDto> GetReportsAsync(PlatformReportsFilterDto filter)
    {
        const string sql = @"
SELECT
    COALESCE(SUM(CASE WHEN sp.PaidAtUtc >= DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1)
                       AND sp.PaidAtUtc < DATEADD(month, 1, DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1))
                      THEN sp.Amount ELSE 0 END), 0) AS CurrentMonthRevenue,
    COALESCE(SUM(CASE WHEN sp.PaidAtUtc >= DATEFROMPARTS(YEAR(SYSUTCDATETIME()), 1, 1)
                       AND sp.PaidAtUtc < DATEADD(year, 1, DATEFROMPARTS(YEAR(SYSUTCDATETIME()), 1, 1))
                      THEN sp.Amount ELSE 0 END), 0) AS CurrentYearRevenue,
    COALESCE(SUM(sp.Amount), 0) AS TotalCollected,
    COUNT(1) AS PaymentCount,
    COALESCE(AVG(sp.Amount), 0) AS AveragePayment
FROM dbo.SubscriptionPayments sp
WHERE sp.PaymentStatus = @Paid
  AND (@From IS NULL OR sp.PaidAtUtc >= @From)
  AND (@To IS NULL OR sp.PaidAtUtc < DATEADD(day, 1, @To))
  AND (@TenantId IS NULL OR sp.TenantId = @TenantId)
  AND (@PlanId IS NULL OR EXISTS (
      SELECT 1 FROM dbo.TenantSubscriptions ts WHERE ts.Id = sp.SubscriptionId AND ts.PlanId = @PlanId
  ));

SELECT
    COALESCE(SUM(CASE WHEN Status = @Active THEN 1 ELSE 0 END), 0) AS Active,
    COALESCE(SUM(CASE WHEN Status = @Trial THEN 1 ELSE 0 END), 0) AS Trial,
    COALESCE(SUM(CASE WHEN Status = @Expired THEN 1 ELSE 0 END), 0) AS Expired,
    COALESCE(SUM(CASE WHEN Status = @Suspended THEN 1 ELSE 0 END), 0) AS Suspended,
    COALESCE(SUM(CASE WHEN Status IN (@Trial, @Active, @PastDue)
              AND EndsAtUtc >= SYSUTCDATETIME()
              AND EndsAtUtc <= DATEADD(day, 7, SYSUTCDATETIME())
             THEN 1 ELSE 0 END), 0) AS ExpiringSoon
FROM dbo.TenantSubscriptions;

SELECT TOP (50)
    sp.Id,
    sp.TenantId,
    t.Name AS ClinicName,
    sp.SubscriptionId,
    ts.PlanId,
    p.Name AS PlanName,
    sp.Amount,
    sp.Currency,
    sp.PaidAtUtc AS PaymentDateUtc,
    sp.PaymentMethod,
    sp.Notes
FROM dbo.SubscriptionPayments sp
JOIN dbo.Tenants t ON t.Id = sp.TenantId
LEFT JOIN dbo.TenantSubscriptions ts ON ts.Id = sp.SubscriptionId
LEFT JOIN dbo.SubscriptionPlans p ON p.Id = ts.PlanId
WHERE sp.PaymentStatus = @Paid
  AND (@From IS NULL OR sp.PaidAtUtc >= @From)
  AND (@To IS NULL OR sp.PaidAtUtc < DATEADD(day, 1, @To))
  AND (@TenantId IS NULL OR sp.TenantId = @TenantId)
  AND (@PlanId IS NULL OR ts.PlanId = @PlanId)
ORDER BY sp.PaidAtUtc DESC, sp.CreatedAtUtc DESC;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            filter.From,
            filter.To,
            filter.TenantId,
            filter.PlanId,
            Paid = SubscriptionPaymentStatus.Paid,
            Active = SubscriptionStatus.Active,
            Trial = SubscriptionStatus.Trial,
            PastDue = SubscriptionStatus.PastDue,
            Expired = SubscriptionStatus.Expired,
            Suspended = SubscriptionStatus.Suspended
        });

        var revenue = await multi.ReadSingleAsync<PlatformRevenueReportDto>();
        var subscriptions = await multi.ReadSingleAsync<PlatformSubscriptionStatusReportDto>();
        var payments = (await multi.ReadAsync<PlatformSubscriptionPaymentDto>()).ToList();
        return new PlatformReportsDto(revenue, subscriptions, payments);
    }

    public async Task<PlatformSettingsDto> GetSettingsAsync()
    {
        const string sql = "SELECT [Key], [Value] FROM dbo.PlatformSettings;";
        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        var rows = await connection.QueryAsync<SettingRow>(sql);
        var values = rows.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        return new PlatformSettingsDto(
            ReadInt(values, "DefaultTrialDays", 14),
            ReadInt(values, "ExpiringSoonThresholdDays", ReadWarningDays(values)),
            ReadInt(values, "DefaultGracePeriodDays", 0),
            ReadBool(values, "AutoSuspendExpiredClinics", false),
            ReadString(values, "Currency", "EGP"),
            ReadNullableString(values, "SupportEmail"),
            ReadNullableString(values, "SupportPhone"),
            ReadNullableString(values, "PaymentMethodsEnabled"),
            ReadDecimal(values, "TaxPercentage"));
    }

    public async Task<PlatformSettingsDto> UpdateSettingsAsync(PlatformSettingsDto settings, Guid? updatedByUserId)
    {
        var values = new Dictionary<string, string?>
        {
            ["DefaultTrialDays"] = settings.TrialDurationDays.ToString(),
            ["ExpiringSoonThresholdDays"] = settings.ExpiringSoonThresholdDays.ToString(),
            ["DefaultGracePeriodDays"] = settings.DefaultGracePeriodDays.ToString(),
            ["AutoSuspendExpiredClinics"] = settings.AutoSuspendExpiredClinics ? "true" : "false",
            ["Currency"] = string.IsNullOrWhiteSpace(settings.CurrencyCode) ? "EGP" : settings.CurrencyCode.Trim().ToUpperInvariant(),
            ["SupportEmail"] = settings.PlatformSupportEmail,
            ["SupportPhone"] = settings.PlatformSupportPhone,
            ["PaymentMethodsEnabled"] = settings.PaymentMethodsEnabled,
            ["TaxPercentage"] = settings.TaxPercentage?.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        const string sql = @"
MERGE dbo.PlatformSettings AS target
USING (VALUES (@Key, @Value)) AS source ([Key], [Value])
ON target.[Key] = source.[Key]
WHEN MATCHED THEN UPDATE SET [Value] = source.[Value], UpdatedAtUtc = SYSUTCDATETIME(), UpdatedByUserId = @UpdatedByUserId
WHEN NOT MATCHED THEN INSERT ([Key], [Value], UpdatedAtUtc, UpdatedByUserId)
VALUES (source.[Key], source.[Value], SYSUTCDATETIME(), @UpdatedByUserId);";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        foreach (var item in values)
        {
            await connection.ExecuteAsync(sql, new { Key = item.Key, Value = item.Value ?? string.Empty, UpdatedByUserId = updatedByUserId });
        }

        await _audit.LogAsync(new AuditEntry
        {
            UserId = updatedByUserId,
            Action = "UpdatePlatformSettings",
            EntityName = "PlatformSettings",
            NewValues = System.Text.Json.JsonSerializer.Serialize(settings),
            CreatedAt = DateTime.UtcNow
        });

        return await GetSettingsAsync();
    }

    private const string ClinicSelect = @"
SELECT
    t.Id,
    t.Name,
    t.Subdomain,
    t.Email,
    t.Phone,
    t.LogoUrl,
    t.[Plan],
    t.TimeZone,
    t.Currency,
    t.IsActive,
    t.CreatedAt,
    t.UpdatedAt,
    COALESCE(u.UsersCount, 0) AS UsersCount,
    COALESCE(p.PatientsCount, 0) AS PatientsCount,
    COALESCE(a.AppointmentsCount, 0) AS AppointmentsCount,
    COALESCE(pay.ClinicRevenue, 0) AS ClinicRevenue,
    ts.Id AS SubscriptionId,
    ts.Status AS SubscriptionStatus,
    ts.StartsAtUtc AS SubscriptionStartDate,
    ts.EndsAtUtc AS SubscriptionEndDate,
    sp.Price AS SubscriptionAmountPaid
FROM dbo.Tenants t
OUTER APPLY
(
    SELECT TOP (1) *
    FROM dbo.TenantSubscriptions latest
    WHERE latest.TenantId = t.Id
    ORDER BY latest.EndsAtUtc DESC, latest.CreatedAtUtc DESC
) ts
LEFT JOIN dbo.SubscriptionPlans sp ON sp.Id = ts.PlanId
OUTER APPLY (SELECT COUNT(1) AS UsersCount FROM dbo.Users WHERE TenantId = t.Id) u
OUTER APPLY (SELECT COUNT(1) AS PatientsCount FROM dbo.Patients WHERE TenantId = t.Id AND IsDeleted = 0) p
OUTER APPLY (SELECT COUNT(1) AS AppointmentsCount FROM dbo.Appointments WHERE TenantId = t.Id AND IsDeleted = 0) a
OUTER APPLY (SELECT COALESCE(SUM(PaidAmount), 0) AS ClinicRevenue FROM dbo.Payments WHERE TenantId = t.Id) pay
";

    private sealed class DashboardCounts
    {
        public int TotalClinics { get; set; }
        public int ActiveClinics { get; set; }
        public int TrialClinics { get; set; }
        public int ExpiredClinics { get; set; }
        public int SuspendedClinics { get; set; }
    }

    private sealed class SettingRow
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    private static int ReadWarningDays(IReadOnlyDictionary<string, string?> values)
    {
        var raw = ReadNullableString(values, "SubscriptionExpiryWarningDays");
        var first = raw?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return int.TryParse(first, out var parsed) ? parsed : 7;
    }

    private static int ReadInt(IReadOnlyDictionary<string, string?> values, string key, int fallback)
        => int.TryParse(ReadNullableString(values, key), out var parsed) ? parsed : fallback;

    private static decimal? ReadDecimal(IReadOnlyDictionary<string, string?> values, string key)
        => decimal.TryParse(ReadNullableString(values, key), out var parsed) ? parsed : null;

    private static bool ReadBool(IReadOnlyDictionary<string, string?> values, string key, bool fallback)
        => bool.TryParse(ReadNullableString(values, key), out var parsed) ? parsed : fallback;

    private static string ReadString(IReadOnlyDictionary<string, string?> values, string key, string fallback)
        => string.IsNullOrWhiteSpace(ReadNullableString(values, key)) ? fallback : ReadNullableString(values, key)!.Trim();

    private static string? ReadNullableString(IReadOnlyDictionary<string, string?> values, string key)
        => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}
