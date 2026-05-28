using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class UpdatePatientDto : CreatePatientDto
    {
        public Guid Id { get; set; }
        public string? RowVersion { get; set; }
    }
}
