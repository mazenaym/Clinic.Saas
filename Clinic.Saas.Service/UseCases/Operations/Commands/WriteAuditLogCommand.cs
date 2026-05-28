using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Operations.Commands;

public class WriteAuditLogCommand
{
    public class Command
    {
        public Guid? TenantId { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public string? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    public class Handler
    {
        private readonly IAuditLogWriter _auditLogWriter;

        public Handler(IAuditLogWriter auditLogWriter)
        {
            _auditLogWriter = auditLogWriter;
        }

        public Task Handle(Command command)
        {
            return _auditLogWriter.WriteAsync(
                command.TenantId,
                command.UserId,
                command.Action,
                command.EntityName,
                command.EntityId,
                command.NewValues,
                command.IpAddress,
                command.UserAgent);
        }
    }
}
