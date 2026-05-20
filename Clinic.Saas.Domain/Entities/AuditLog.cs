using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class AuditLog
    {
        public long Id { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public string? OldValues { get; set; } // JSON
        public string? NewValues { get; set; } // JSON
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
