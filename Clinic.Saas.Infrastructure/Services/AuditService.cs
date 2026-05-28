using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Dapper;

namespace Clinic.Saas.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AuditService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task LogAsync(AuditEntry entry)
    {
        try
        {
            const string sql = @"
INSERT INTO dbo.AuditLogs
(
    TenantId, UserId, Action, EntityName, EntityId,
    OldValues, NewValues, IpAddress, UserAgent, CreatedAt
)
VALUES
(
    @TenantId, @UserId, @Action, @EntityName, @EntityId,
    @OldValues, @NewValues, @IpAddress, @UserAgent, @CreatedAt
);";

            using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            await connection.ExecuteAsync(sql, new
            {
                entry.TenantId,
                entry.UserId,
                entry.Action,
                entry.EntityName,
                entry.EntityId,
                entry.OldValues,
                entry.NewValues,
                entry.IpAddress,
                entry.UserAgent,
                CreatedAt = entry.CreatedAt == default ? DateTime.UtcNow : entry.CreatedAt
            });
        }
        catch
        {
            // Audit logging must never break the business operation.
        }
    }
}
