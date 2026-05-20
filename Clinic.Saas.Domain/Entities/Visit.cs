using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class Visit
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid PatientId { get; set; }
        public Guid? AppointmentId { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime VisitDate { get; set; }
        public VisitType VisitType { get; set; }
        public string ChiefComplaint { get; set; } = string.Empty;
        public string? VitalSigns { get; set; } // JSON
        public string? ClinicalNotes { get; set; }
        public string? Diagnosis { get; set; }
        public string? DiagnosisCode { get; set; } // ICD-10
        public string? DifferentialDx { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string? FollowUpNotes { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
    }
}
