using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class CreateOnlineBookingDto
    {
        public string PatientName { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public string? PatientEmail { get; set; }
        public DateTime RequestedDate { get; set; }
        public TimeSpan RequestedTime { get; set; }
        public Guid? DoctorId { get; set; }
        public string? Complaint { get; set; }
    }
}

