using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class VitalSignsDto
    {
        public string? BloodPressure { get; set; }  // "120/80"
        public decimal? Temperature { get; set; }   // 37.2
        public decimal? Weight { get; set; }        // kg
        public int? Height { get; set; }            // cm
        public int? Pulse { get; set; }             // bpm
        public int? SpO2 { get; set; }              // %
        public decimal? RBS { get; set; }           // mg/dL
    }
}
