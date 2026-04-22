using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class ClinicSettings
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string WorkingDays { get; set; } = "0111110"; // Sun-Sat bitmask
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
        public int SlotDurationMin { get; set; } = 20;
        public decimal ConsultFee { get; set; }
        public bool SmsEnabled { get; set; }
        public bool WhatsappEnabled { get; set; }
        public bool EmailEnabled { get; set; }
        public string Language { get; set; } = "ar";
        public decimal TaxPct { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public Tenant Tenant { get; set; } = null!;
    }
}
