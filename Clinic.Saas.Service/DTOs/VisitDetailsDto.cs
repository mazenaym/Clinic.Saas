using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class VisitDetailsDto : VisitDto
    {
        public string? ClinicalNotes { get; set; }
        public string? DifferentialDx { get; set; }
        public string? FollowUpNotes { get; set; }
        public List<PrescriptionDto> Prescriptions { get; set; } = new();
        public List<LabRequestDto> LabRequests { get; set; } = new();
        public InvoiceDto? Invoice { get; set; }
    }
}
