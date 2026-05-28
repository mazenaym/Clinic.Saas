using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;

public class ClinicSettingsRepository : IClinicSettingsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ClinicSettingsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> IsWhatsappEnabledAsync(Guid tenantId)
    {
        const string sql = @"
SELECT COALESCE(
    (SELECT WhatsappEnabled FROM dbo.ClinicSettings WHERE TenantId = @TenantId),
    CAST(0 AS bit)
);";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(sql, new { TenantId = tenantId });
    }
}
