using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class PatientDto
    {
        public Guid Id { get; set; }
        public string PatientCode { get; set; } 
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public int? Age => DateOfBirth.HasValue
            ? DateTime.Now.Year - DateOfBirth.Value.Year
            : null;
        public string Gender { get; set; } = string.Empty;
        public string? BloodType { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? DrugAllergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? InsuranceCompany { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
