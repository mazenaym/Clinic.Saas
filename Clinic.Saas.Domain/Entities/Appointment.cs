using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class Appointment
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public AppointmentStatus Status { get; set; }
        public AppointmentType Type { get; set; }
        public AppointmentSource Source { get; set; }
        public string? Notes { get; set; }
        public string? CancelReason { get; set; }
        public bool ReminderSent { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; } = [];
    }
}
