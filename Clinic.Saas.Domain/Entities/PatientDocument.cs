using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Clinic.Saas.Domain.Entities
{
    public class PatientDocument
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid PatientId { get; set; }
        public Guid? VisitId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public int? FileSizeKb { get; set; }
        public string FileType { get; set; } = string.Empty;
        public DocumentType DocumentType { get; set; }
        public string? Description { get; set; }
        public Guid? UploadedBy { get; set; }
        public string? UploadedByName { get; set; }
        public DateTime UploadedAt { get; set; }
        public byte[] RowVersion { get; set; } = [];
    }
}
