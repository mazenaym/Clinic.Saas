using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? LogoUrl { get; set; }
        public PlanType Plan { get; set; }
        public string TimeZone { get; set; } = "Africa/Cairo";
        public string Currency { get; set; } = "EGP";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Patient> Patients { get; set; } = new List<Patient>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ClinicSettings? ClinicSettings { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
