using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string? Phone { get; set; }
        public string? Specialty { get; set; }
        public string? LicenseNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockedUntil { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public Tenant Tenant { get; set; } = null!;
        public ICollection<Appointment> AppointmentsAsDoctor { get; set; } = new List<Appointment>();
        public ICollection<Visit> VisitsAsDoctor { get; set; } = new List<Visit>();
        public ICollection<Prescription> PrescriptionsAsDoctor { get; set; } = new List<Prescription>();
    }
}
