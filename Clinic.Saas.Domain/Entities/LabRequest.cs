using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class LabRequest
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid VisitId { get; set; }
        public Guid PatientId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string? TestCode { get; set; }
        public LabRequestStatus Status { get; set; }
        public string? Result { get; set; }
        public DateTime? ResultDate { get; set; }
        public string? ResultFileUrl { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public Tenant Tenant { get; set; } = null!;
        public Visit Visit { get; set; } = null!;
        public Patient Patient { get; set; } = null!;
    }
}
