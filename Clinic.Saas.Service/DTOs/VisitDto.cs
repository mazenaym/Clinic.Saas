using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class VisitDto
    {
        public Guid Id { get; set; }
        public DateTime VisitDate { get; set; }
        public string VisitType { get; set; } = string.Empty;
        public string ChiefComplaint { get; set; } = string.Empty;
        public VitalSignsDto? VitalSigns { get; set; }
        public string? Diagnosis { get; set; }
        public string? DiagnosisCode { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
    }
}
