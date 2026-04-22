using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class CreateLabRequestDto
    {
        public Guid VisitId { get; set; }
        public Guid PatientId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string? TestCode { get; set; }
        public string? Notes { get; set; }
    }
}
