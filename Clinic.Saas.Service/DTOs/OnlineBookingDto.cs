using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class OnlineBookingDto
    {
        public Guid Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public DateTime RequestedDate { get; set; }
        public TimeSpan RequestedTime { get; set; }
        public string? DoctorName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ConfirmCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
