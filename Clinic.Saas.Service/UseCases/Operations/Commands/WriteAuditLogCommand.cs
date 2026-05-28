using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.DTOs;

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
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    public class Handler
    {
        private readonly IAuditService _auditService;

        public Handler(IAuditService auditService)
        {
            _auditService = auditService;
        }

        public Task Handle(Command command)
        {
            return _auditService.LogAsync(new AuditEntry
            {
                TenantId = command.TenantId,
                UserId = command.UserId,
                Action = command.Action,
                EntityName = command.EntityName,
                EntityId = command.EntityId,
                OldValues = command.OldValues,
                NewValues = command.NewValues,
                IpAddress = command.IpAddress,
                UserAgent = command.UserAgent
            });
        }
    }
}
