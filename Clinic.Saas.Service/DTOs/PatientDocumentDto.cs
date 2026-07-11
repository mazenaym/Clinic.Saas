using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class PatientDocumentDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int FileSizeKb { get; set; }
        public string FileType { get; set; } = string.Empty;
        public short DocumentType { get; set; }
        public string? Description { get; set; }
        public Guid? UploadedBy { get; set; }
        public string? UploadedByName { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? RowVersion { get; set; }
    }
}
