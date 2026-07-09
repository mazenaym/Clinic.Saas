using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Specialty { get; set; }
        public bool IsActive { get; set; }
        public string? SubscriptionStatus { get; set; }
        public DateTime? SubscriptionEndsAtUtc { get; set; }
        public bool IsInGracePeriod { get; set; }
        public int DaysRemaining { get; set; }
    }
}
