using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class MonthlyReportDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalPatients { get; set; }
        public int NewPatients { get; set; }
        public int TotalVisits { get; set; }
        public decimal TotalRevenue { get; set; }
        public Dictionary<string, int> VisitsByDoctor { get; set; } = new();
        public Dictionary<string, int> VisitsByType { get; set; } = new();
    }
}
