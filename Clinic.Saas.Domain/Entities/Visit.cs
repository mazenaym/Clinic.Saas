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

        // Navigation Properties
        public Tenant Tenant { get; set; } = null!;
        public Patient Patient { get; set; } = null!;
        public Appointment? Appointment { get; set; }
        public User Doctor { get; set; } = null!;
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public ICollection<LabRequest> LabRequests { get; set; } = new List<LabRequest>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<PatientDocument> Documents { get; set; } = new List<PatientDocument>();
    }
}
