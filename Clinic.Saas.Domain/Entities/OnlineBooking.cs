using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class OnlineBooking
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? AppointmentId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public string? PatientEmail { get; set; }
        public DateTime RequestedDate { get; set; }
        public TimeSpan RequestedTime { get; set; }
        public Guid? DoctorId { get; set; }
        public string? Complaint { get; set; }
        public OnlineBookingStatus Status { get; set; }
        public string ConfirmCode { get; set; } = string.Empty;
        public string? RejectReason { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public Tenant Tenant { get; set; } = null!;
        public Appointment? Appointment { get; set; }
        public User? Doctor { get; set; }
    }
}
