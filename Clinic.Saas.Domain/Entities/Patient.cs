using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class Patient
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string PatientCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public string? BloodType { get; set; }
        public string? NationalId { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? MedicalHistory { get; set; }
        public string? DrugAllergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? InsuranceCompany { get; set; }
        public string? InsuranceNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }

        // Navigation Properties
        public Tenant Tenant { get; set; } = null!;
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<Visit> Visits { get; set; } = new List<Visit>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<PatientDocument> Documents { get; set; } = new List<PatientDocument>();
    }
}
