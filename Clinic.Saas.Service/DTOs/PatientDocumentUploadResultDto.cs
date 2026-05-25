using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class PatientDocumentUploadResultDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int FileSizeKb { get; set; }
        public string FileType { get; set; } = string.Empty;
    }
}
