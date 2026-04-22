using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class PrescriptionDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }
        public string? QrCode { get; set; }
        public string? PdfUrl { get; set; }
        public bool SentViaWhatsapp { get; set; }
        public List<PrescriptionItemDto> Items { get; set; } = new();
        public string DoctorName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
    }
}
