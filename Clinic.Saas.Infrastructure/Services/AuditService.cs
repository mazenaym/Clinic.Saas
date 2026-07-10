using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Clinic.Saas.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly Microsoft.Extensions.Logging.ILogger<AuditService> _logger;

    public AuditService(IDbConnectionFactory connectionFactory, Microsoft.Extensions.Logging.ILogger<AuditService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit logging failed for {Action} {EntityName} {EntityId}", entry.Action, entry.EntityName, entry.EntityId);
        }
    }
}
