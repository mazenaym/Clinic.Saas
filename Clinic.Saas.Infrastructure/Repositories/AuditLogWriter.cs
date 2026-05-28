using Clinic.Saas.Service.Interfaces;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;

public class AuditLogWriter : IAuditLogWriter
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AuditLogWriter(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task WriteAsync(
        Guid? tenantId,
        Guid? userId,
        string action,
        string entityName,
        Guid? entityId,
        string? newValues,
        string? ipAddress,
        string? userAgent)
    {
        const string sql = @"
INSERT INTO dbo.AuditLogs (TenantId, UserId, Action, EntityName, EntityId, NewValues, IpAddress, UserAgent, CreatedAt)
VALUES (@TenantId, @UserId, @Action, @EntityName, @EntityId, @NewValues, @IpAddress, @UserAgent, SYSUTCDATETIME());";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });
    }
}
