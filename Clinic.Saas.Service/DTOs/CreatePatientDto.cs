using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class CreatePatientDto
    {
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
    }
}
