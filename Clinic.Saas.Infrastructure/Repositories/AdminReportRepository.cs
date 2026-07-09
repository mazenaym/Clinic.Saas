using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;

public class AdminReportRepository : IAdminReportRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AdminReportRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<ClinicUsageMetricDto>> GetClinicUsageMetricsAsync()
    {
        const string sql = @"
SELECT t.Id, t.Name, t.Subdomain,
       (SELECT COUNT(1) FROM dbo.Users u WHERE u.TenantId = t.Id AND u.IsActive = 1) AS UsersCount,
       (SELECT COUNT(1) FROM dbo.Patients p WHERE p.TenantId = t.Id AND p.IsDeleted = 0) AS PatientsCount,
       (SELECT COUNT(1) FROM dbo.Appointments a WHERE a.TenantId = t.Id AND a.IsDeleted = 0) AS AppointmentsCount
FROM dbo.Tenants t
ORDER BY t.CreatedAt DESC;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.QueryAsync<ClinicUsageMetricDto>(sql);
    }

    public async Task<IEnumerable<SubscriptionRevenueDto>> GetSubscriptionRevenueAsync()
    {
        const string sql = @"
SELECT YEAR(CreatedAt) AS [Year],
       MONTH(CreatedAt) AS [Month],
       SUM(AmountPaid) AS Revenue,
       COUNT(1) AS SubscriptionCount
FROM dbo.Subscriptions
GROUP BY YEAR(CreatedAt), MONTH(CreatedAt)
ORDER BY [Year] DESC, [Month] DESC;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.QueryAsync<SubscriptionRevenueDto>(sql);
    }

    public async Task<IEnumerable<ExpiringSubscriptionDto>> GetExpiringSubscriptionsAsync(int days)
    {
        const string sql = @"
SELECT t.Name, t.Subdomain, s.[Plan], s.EndDate, s.Status
FROM dbo.Subscriptions s
INNER JOIN dbo.Tenants t ON t.Id = s.TenantId
WHERE s.EndDate >= SYSUTCDATETIME()
  AND s.EndDate < DATEADD(day, @Days, SYSUTCDATETIME())
ORDER BY s.EndDate;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.QueryAsync<ExpiringSubscriptionDto>(sql, new { Days = days });
    }

    public async Task<IEnumerable<AuditLogDto>> GetActivityLogAsync(int take, Guid? tenantId)
    {
        var sql = tenantId.HasValue
            ? @"
SELECT TOP (@Take) Id, TenantId, UserId, Action, EntityName, EntityId, NewValues, CreatedAt
FROM dbo.AuditLogs
WHERE TenantId = @TenantId
ORDER BY CreatedAt DESC;"
            : @"
SELECT TOP (@Take) Id, TenantId, UserId, Action, EntityName, EntityId, NewValues, CreatedAt
FROM dbo.AuditLogs
ORDER BY CreatedAt DESC;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.QueryAsync<AuditLogDto>(sql, new { Take = take, TenantId = tenantId });
    }
}
