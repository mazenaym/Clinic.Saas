using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class LabRequestDto
    {
        public Guid Id { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string? TestCode { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Result { get; set; }
        public DateTime? ResultDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
