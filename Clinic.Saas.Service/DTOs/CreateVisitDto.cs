using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class CreateVisitDto
    {
        public Guid PatientId { get; set; }
        public Guid? AppointmentId { get; set; }
        public Guid DoctorId { get; set; }
        public VisitType VisitType { get; set; }
        public string ChiefComplaint { get; set; } = string.Empty;
        public VitalSignsDto? VitalSigns { get; set; }
        public string? ClinicalNotes { get; set; }
        public string? Diagnosis { get; set; }
        public string? DiagnosisCode { get; set; }
        public DateTime? FollowUpDate { get; set; }
    }
}
