using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class CreatePrescriptionDto
    {
        public Guid VisitId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string? Notes { get; set; }
        public List<CreatePrescriptionItemDto> Items { get; set; } = new();
    }
}
