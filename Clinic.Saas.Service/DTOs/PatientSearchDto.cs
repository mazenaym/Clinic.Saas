using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class PatientSearchDto
    {
        public string? SearchTerm { get; set; }
        public Gender? Gender { get; set; }
        public string? BloodType { get; set; }
        public bool? HasDrugAllergies { get; set; }
        public DateTime? RegisteredFrom { get; set; }
        public DateTime? RegisteredTo { get; set; }
    }
}
