using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? PatientId { get; set; }
        public Guid? AppointmentId { get; set; }
        public Guid? UserId { get; set; }
        public NotificationType Type { get; set; }
        public NotificationChannel Channel { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;
        public NotificationStatus Status { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ProviderRef { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public Tenant Tenant { get; set; } = null!;
        public Patient? Patient { get; set; }
        public Appointment? Appointment { get; set; }
        public User? User { get; set; }
    }
}
