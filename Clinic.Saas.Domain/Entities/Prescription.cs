using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class Prescription
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid VisitId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string? Notes { get; set; }
        public string? QrCode { get; set; }
        public string? PdfUrl { get; set; }
        public bool SentViaWhatsapp { get; set; }
        public bool SentViaSms { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
    }
}
