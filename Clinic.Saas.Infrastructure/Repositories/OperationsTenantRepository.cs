using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;

public class OperationsTenantRepository : IOperationsTenantRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OperationsTenantRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TenantSubscriptionStatusDto?> GetTenantStatusAsync(Guid tenantId)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT
    COALESCE(t.SubscriptionState, N'Trial') AS State,
    t.TrialEndsAt,
    (SELECT TOP 1 EndDate FROM dbo.Subscriptions WHERE TenantId = t.Id AND Status IN (1,4) ORDER BY EndDate DESC) AS SubscriptionEndsAt,
    COALESCE(t.MaxUsers, 2) AS MaxUsers,
    COALESCE(t.MaxPatientsPerMonth, 200) AS MaxPatientsPerMonth
FROM dbo.Tenants t
WHERE t.Id = @TenantId;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.QueryFirstOrDefaultAsync<TenantSubscriptionStatusDto>(sql, new { TenantId = tenantId });
    }

    public async Task<UpdateClinicSettingsDto> GetSettingsAsync(Guid tenantId)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT WorkingDays, OpenTime, CloseTime, SlotDurationMin, ConsultFee, SmsEnabled, WhatsappEnabled, EmailEnabled, [Language], TaxPct
FROM dbo.ClinicSettings
WHERE TenantId = @TenantId;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.QueryFirstOrDefaultAsync<UpdateClinicSettingsDto>(sql, new { TenantId = tenantId })
            ?? new UpdateClinicSettingsDto();
    }

    public async Task UpsertSettingsAsync(Guid tenantId, UpdateClinicSettingsDto settings)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
MERGE dbo.ClinicSettings AS target
USING (SELECT @TenantId AS TenantId) AS source
ON target.TenantId = source.TenantId
WHEN MATCHED THEN UPDATE SET
    WorkingDays = @WorkingDays,
    OpenTime = @OpenTime,
    CloseTime = @CloseTime,
    SlotDurationMin = @SlotDurationMin,
    ConsultFee = @ConsultFee,
    SmsEnabled = @SmsEnabled,
    WhatsappEnabled = @WhatsappEnabled,
    EmailEnabled = @EmailEnabled,
    [Language] = @Language,
    TaxPct = @TaxPct,
    UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT
    (Id, TenantId, WorkingDays, OpenTime, CloseTime, SlotDurationMin, ConsultFee, SmsEnabled, WhatsappEnabled, EmailEnabled, [Language], TaxPct, UpdatedAt)
VALUES
    (NEWID(), @TenantId, @WorkingDays, @OpenTime, @CloseTime, @SlotDurationMin, @ConsultFee, @SmsEnabled, @WhatsappEnabled, @EmailEnabled, @Language, @TaxPct, SYSUTCDATETIME());";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            settings.WorkingDays,
            settings.OpenTime,
            settings.CloseTime,
            settings.SlotDurationMin,
            settings.ConsultFee,
            settings.SmsEnabled,
            settings.WhatsappEnabled,
            settings.EmailEnabled,
            settings.Language,
            settings.TaxPct
        });
    }

    private static void EnsureTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required.");
        }
    }
}
