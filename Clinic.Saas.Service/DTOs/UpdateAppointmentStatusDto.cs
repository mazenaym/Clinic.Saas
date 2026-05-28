using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class UpdateAppointmentStatusDto
    {
        public Guid Id { get; set; }
        public AppointmentStatus Status { get; set; }
        public string? CancelReason { get; set; }
        public string? RowVersion { get; set; }
    }
}
