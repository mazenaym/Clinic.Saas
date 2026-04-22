using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class ClinicSettingsDto
    {
        public string WorkingDays { get; set; }
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
        public int SlotDurationMin { get; set; }
        public decimal ConsultFee { get; set; }
        public bool SmsEnabled { get; set; }
        public bool WhatsappEnabled { get; set; }
        public bool EmailEnabled { get; set; }
        public string Language { get; set; } = "ar";
        public decimal TaxPct { get; set; }
    }
}
