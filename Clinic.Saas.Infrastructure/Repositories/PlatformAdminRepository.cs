using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Dapper;
using System.Data;

namespace Clinic.Saas.Infrastructure.Repositories;

public class PlatformAdminRepository : IPlatformAdminRepository
{
    private readonly DapperContext _context;

    public PlatformAdminRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardStatsDto> GetDashboardStatsAsync(DateTime utcNow)
    {
        const string statsSql = @"
SELECT
    COUNT(1) AS TotalClinics,
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveClinics,
    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS InactiveClinics
FROM dbo.Tenants;

SELECT COUNT(1) FROM dbo.Users;
SELECT COUNT(1) FROM dbo.Patients WHERE IsDeleted = 0;

SELECT
    SUM(CASE WHEN ts.[Status] = 1 THEN 1 ELSE 0 END) AS ActiveSubscriptions,
    SUM(CASE WHEN ts.[Status] = 4 THEN 1 ELSE 0 END) AS TrialSubscriptions,
    SUM(CASE WHEN ts.[Status] = 3 THEN 1 ELSE 0 END) AS ExpiredSubscriptions,
    COALESCE(SUM(CASE WHEN sp.PaymentStatus = 2 THEN sp.Amount ELSE 0 END), 0) AS TotalRevenue,
    COALESCE(SUM(CASE WHEN sp.PaymentStatus = 2 AND sp.PaidAtUtc >= @MonthStart THEN sp.Amount ELSE 0 END), 0) AS MonthlyRevenue,
    COALESCE(SUM(CASE WHEN sp.PaymentStatus = 2 AND CAST(sp.PaidAtUtc AS date) = @Today THEN sp.Amount ELSE 0 END), 0) AS TodayRevenue
FROM dbo.TenantSubscriptions ts
LEFT JOIN dbo.SubscriptionPayments sp ON sp.SubscriptionId = ts.Id;";

        using var connection = _context.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(statsSql, new
        {
            MonthStart = new DateTime(utcNow.Year, utcNow.Month, 1),
            Today = utcNow.Date
        });

        var stats = await multi.ReadSingleAsync<AdminDashboardStatsDto>();
        stats.TotalUsers = await multi.ReadSingleAsync<int>();
        stats.TotalPatients = await multi.ReadSingleAsync<int>();

        var subscriptionStats = await multi.ReadSingleAsync<AdminDashboardStatsDto>();
        stats.ActiveSubscriptions = subscriptionStats.ActiveSubscriptions;
        stats.TrialSubscriptions = subscriptionStats.TrialSubscriptions;
        stats.ExpiredSubscriptions = subscriptionStats.ExpiredSubscriptions;
        stats.TotalRevenue = subscriptionStats.TotalRevenue;
        stats.MonthlyRevenue = subscriptionStats.MonthlyRevenue;
        stats.TodayRevenue = subscriptionStats.TodayRevenue;

        stats.RecentClinics = (await GetClinicsInternalAsync(connection, "ORDER BY t.CreatedAt DESC OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY")).ToList();
        return stats;
    }

    public async Task<IEnumerable<AdminClinicDto>> GetClinicsAsync()
    {
        using var connection = _context.CreateConnection();
        return await GetClinicsInternalAsync(connection, "ORDER BY t.CreatedAt DESC");
    }

    public async Task<AdminClinicDto?> GetClinicByIdAsync(Guid clinicId)
    {
        using var connection = _context.CreateConnection();
        return await GetClinicByIdInternalAsync(connection, clinicId);
    }

    public async Task<bool> SubdomainExistsAsync(string subdomain, Guid? excludeTenantId = null)
    {
        const string sql = @"
SELECT COUNT(1)
FROM dbo.Tenants
WHERE LOWER(Subdomain) = LOWER(@Subdomain)
  AND (@ExcludeTenantId IS NULL OR Id <> @ExcludeTenantId);";

        using var connection = _context.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Subdomain = subdomain, ExcludeTenantId = excludeTenantId });
        return count > 0;
    }

    public async Task<bool> SuperAdminExistsAsync()
    {
        const string sql = @"SELECT COUNT(1) FROM dbo.Users WHERE [Role] = 0;";

        using var connection = _context.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql);
        return count > 0;
    }

    public async Task<AdminClinicDto?> BootstrapSuperAdminAsync(Tenant platformTenant, User superAdmin)
    {
        if (platformTenant.Id == Guid.Empty)
        {
            platformTenant.Id = Guid.NewGuid();
        }

        if (superAdmin.Id == Guid.Empty)
        {
            superAdmin.Id = Guid.NewGuid();
        }

        superAdmin.TenantId = platformTenant.Id;

        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            const string guardSql = @"SELECT COUNT(1) FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE [Role] = 0;";
            var existingSuperAdmins = await connection.ExecuteScalarAsync<int>(guardSql, transaction: transaction);
            if (existingSuperAdmins > 0)
            {
                transaction.Rollback();
                return null;
            }

            const string tenantSql = @"
INSERT INTO dbo.Tenants
(Id, Name, Subdomain, Email, Phone, LogoUrl, [Plan], TimeZone, Currency, IsActive, CreatedAt, UpdatedAt)
VALUES
(@Id, @Name, @Subdomain, @Email, @Phone, @LogoUrl, @Plan, @TimeZone, @Currency, @IsActive, @CreatedAt, @UpdatedAt);";

            const string userSql = @"
INSERT INTO dbo.Users
(
    Id, TenantId, FullName, Email, PasswordHash, [Role], Phone, Specialty,
    LicenseNumber, AvatarUrl, RefreshToken, RefreshTokenExpiry, FailedLoginAttempts,
    LockedUntil, IsActive, CreatedAt, UpdatedAt
)
VALUES
(
    @Id, @TenantId, @FullName, @Email, @PasswordHash, @Role, @Phone, @Specialty,
    @LicenseNumber, @AvatarUrl, @RefreshToken, @RefreshTokenExpiry, @FailedLoginAttempts,
    @LockedUntil, @IsActive, @CreatedAt, @UpdatedAt
);";

            await connection.ExecuteAsync(tenantSql, platformTenant, transaction);
            await connection.ExecuteAsync(userSql, superAdmin, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        var created = await GetClinicByIdInternalAsync(connection, platformTenant.Id);
        return created ?? throw new InvalidOperationException("Platform tenant was not found after bootstrap.");
    }

    public async Task<AdminClinicDto> CreateClinicAsync(Tenant tenant, User owner, CreateSubscriptionRequest subscription, ClinicSettingsDto settings)
    {
        if (tenant.Id == Guid.Empty)
        {
            tenant.Id = Guid.NewGuid();
        }

        if (owner.Id == Guid.Empty)
        {
            owner.Id = Guid.NewGuid();
        }

        owner.TenantId = tenant.Id;

        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            const string tenantSql = @"
INSERT INTO dbo.Tenants
(Id, Name, Subdomain, Email, Phone, LogoUrl, [Plan], TimeZone, Currency, IsActive, CreatedAt, UpdatedAt)
VALUES
(@Id, @Name, @Subdomain, @Email, @Phone, @LogoUrl, @Plan, @TimeZone, @Currency, @IsActive, @CreatedAt, @UpdatedAt);";

            const string userSql = @"
INSERT INTO dbo.Users
(
    Id, TenantId, FullName, Email, PasswordHash, [Role], Phone, Specialty,
    LicenseNumber, AvatarUrl, RefreshToken, RefreshTokenExpiry, FailedLoginAttempts,
    LockedUntil, IsActive, CreatedAt, UpdatedAt
)
VALUES
(
    @Id, @TenantId, @FullName, @Email, @PasswordHash, @Role, @Phone, @Specialty,
    @LicenseNumber, @AvatarUrl, @RefreshToken, @RefreshTokenExpiry, @FailedLoginAttempts,
    @LockedUntil, @IsActive, @CreatedAt, @UpdatedAt
);";

            const string subscriptionSql = @"
DECLARE @PlanId UNIQUEIDENTIFIER = (
    SELECT TOP 1 p.Id FROM dbo.SubscriptionPlans p
    WHERE p.IsActive = 1
      AND ((@Plan = 1 AND p.Code IN (N'TRIAL', N'BASIC_MONTHLY'))
        OR (@Plan = 2 AND p.Code = N'PRO_MONTHLY')
        OR (@Plan = 3 AND p.Code = N'ANNUAL'))
    ORDER BY CASE WHEN @Plan = 1 AND p.Code = N'TRIAL' THEN 0 ELSE 1 END
);
DECLARE @TenantSubId UNIQUEIDENTIFIER = NEWID();
INSERT INTO dbo.TenantSubscriptions
(Id, TenantId, PlanId, Status, StartsAtUtc, EndsAtUtc, AutoRenew, GracePeriodDays, Notes, CreatedByUserId, CreatedAtUtc)
VALUES
(@TenantSubId, @TenantId, @PlanId, @Status, @StartDate, @EndDate, 0, 0, @Notes, @CreatedBy, @CreatedAt);

INSERT INTO dbo.SubscriptionPayments
(Id, TenantId, SubscriptionId, Amount, Currency, PaymentStatus, PaymentMethod, PaidAtUtc, CreatedAtUtc, Notes, CreatedByUserId)
VALUES
(NEWID(), @TenantId, @TenantSubId, @AmountPaid, N'EGP', 2, @PaymentRef, @StartDate, @CreatedAt, @Notes, @CreatedBy);";

            const string settingsSql = @"
INSERT INTO dbo.ClinicSettings
(
    Id, TenantId, WorkingDays, OpenTime, CloseTime, SlotDurationMin, ConsultFee,
    SmsEnabled, WhatsappEnabled, EmailEnabled, [Language], TaxPct, UpdatedAt
)
VALUES
(
    @Id, @TenantId, @WorkingDays, @OpenTime, @CloseTime, @SlotDurationMin, @ConsultFee,
    @SmsEnabled, @WhatsappEnabled, @EmailEnabled, @Language, @TaxPct, @UpdatedAt
);";

            await connection.ExecuteAsync(tenantSql, tenant, transaction);
            await connection.ExecuteAsync(userSql, owner, transaction);
            await connection.ExecuteAsync(subscriptionSql, new
            {
                Plan = (short)subscription.Plan,
                TenantId = tenant.Id,
                Status = (short)subscription.Status,
                subscription.StartDate,
                subscription.EndDate,
                subscription.AmountPaid,
                subscription.PaymentRef,
                subscription.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = owner.Id
            }, transaction);
            await connection.ExecuteAsync(settingsSql, new
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                settings.WorkingDays,
                settings.OpenTime,
                settings.CloseTime,
                settings.SlotDurationMin,
                settings.ConsultFee,
                settings.SmsEnabled,
                settings.WhatsappEnabled,
                settings.EmailEnabled,
                settings.Language,
                settings.TaxPct,
                UpdatedAt = tenant.UpdatedAt
            }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        var created = await GetClinicByIdInternalAsync(connection, tenant.Id);
        return created ?? throw new InvalidOperationException("Clinic was not found after creation.");
    }

    public async Task UpdateClinicAsync(Tenant tenant)
    {
        const string sql = @"
UPDATE dbo.Tenants
SET Name = @Name,
    Subdomain = @Subdomain,
    Email = @Email,
    Phone = @Phone,
    LogoUrl = @LogoUrl,
    [Plan] = @Plan,
    TimeZone = @TimeZone,
    Currency = @Currency,
    IsActive = @IsActive,
    UpdatedAt = @UpdatedAt
WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, tenant);
    }

    public async Task SetClinicStatusAsync(Guid clinicId, bool isActive)
    {
        const string sql = @"
UPDATE dbo.Tenants
SET IsActive = @IsActive,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @ClinicId;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { ClinicId = clinicId, IsActive = isActive });
    }

    public async Task AddSubscriptionAsync(CreateSubscriptionRequest subscription)
    {
        const string sql = @"
DECLARE @PlanId UNIQUEIDENTIFIER = (
    SELECT TOP 1 p.Id FROM dbo.SubscriptionPlans p
    WHERE p.IsActive = 1
      AND ((@Plan = 1 AND p.Code IN (N'TRIAL', N'BASIC_MONTHLY'))
        OR (@Plan = 2 AND p.Code = N'PRO_MONTHLY')
        OR (@Plan = 3 AND p.Code = N'ANNUAL'))
    ORDER BY CASE WHEN @Plan = 1 AND p.Code = N'TRIAL' THEN 0 ELSE 1 END
);
DECLARE @TenantSubId UNIQUEIDENTIFIER = NEWID();

UPDATE dbo.TenantSubscriptions
SET Status = 5, CancelledAtUtc = @Now, UpdatedAtUtc = @Now
WHERE TenantId = @TenantId AND Status IN (1, 2, 4);

INSERT INTO dbo.TenantSubscriptions
(Id, TenantId, PlanId, Status, StartsAtUtc, EndsAtUtc, AutoRenew, GracePeriodDays, Notes, CreatedByUserId, CreatedAtUtc)
VALUES
(@TenantSubId, @TenantId, @PlanId, @Status, @StartDate, @EndDate, 0, 0, @Notes, NULL, @Now);

INSERT INTO dbo.SubscriptionPayments
(Id, TenantId, SubscriptionId, Amount, Currency, PaymentStatus, PaymentMethod, PaidAtUtc, CreatedAtUtc, Notes, CreatedByUserId)
VALUES
(NEWID(), @TenantId, @TenantSubId, @AmountPaid, N'EGP', 2, @PaymentRef, @StartDate, @Now, @Notes, NULL);";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            Plan = (short)subscription.Plan,
            subscription.TenantId,
            Status = (short)subscription.Status,
            subscription.StartDate,
            subscription.EndDate,
            subscription.AmountPaid,
            subscription.PaymentRef,
            subscription.Notes,
            Now = DateTime.UtcNow
        });
    }

    private static Task<IEnumerable<AdminClinicDto>> GetClinicsInternalAsync(IDbConnection connection, string orderBy)
    {
        var sql = ClinicSelect + Environment.NewLine + orderBy + ";";
        return connection.QueryAsync<AdminClinicDto>(sql);
    }

    private static Task<AdminClinicDto?> GetClinicByIdInternalAsync(IDbConnection connection, Guid clinicId)
    {
        var sql = ClinicSelect + @"
WHERE t.Id = @ClinicId;";

        return connection.QueryFirstOrDefaultAsync<AdminClinicDto>(sql, new { ClinicId = clinicId });
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
    COALESCE(inv.ClinicRevenue, 0) AS ClinicRevenue,
    ts.Id AS SubscriptionId,
    ts.Status AS SubscriptionStatus,
    ts.StartsAtUtc AS SubscriptionStartDate,
    ts.EndsAtUtc AS SubscriptionEndDate,
    COALESCE(subscriptionPayments.TotalPaidAmount, 0) AS SubscriptionAmountPaid
FROM dbo.Tenants t
OUTER APPLY
(
    SELECT TOP (1) *
    FROM dbo.TenantSubscriptions latest
    WHERE latest.TenantId = t.Id
    ORDER BY latest.EndsAtUtc DESC, latest.CreatedAtUtc DESC
) ts
OUTER APPLY
(
    SELECT COALESCE(SUM(payment.Amount), 0) AS TotalPaidAmount
    FROM dbo.SubscriptionPayments payment
    WHERE payment.SubscriptionId = ts.Id
      AND payment.PaymentStatus = 2
) subscriptionPayments
OUTER APPLY (SELECT COUNT(1) AS UsersCount FROM dbo.Users WHERE TenantId = t.Id) u
OUTER APPLY (SELECT COUNT(1) AS PatientsCount FROM dbo.Patients WHERE TenantId = t.Id AND IsDeleted = 0) p
OUTER APPLY (SELECT COUNT(1) AS AppointmentsCount FROM dbo.Appointments WHERE TenantId = t.Id AND IsDeleted = 0) a
OUTER APPLY (SELECT COALESCE(SUM(GrandTotal), 0) AS ClinicRevenue FROM dbo.Invoices WHERE TenantId = t.Id) inv
";
}
