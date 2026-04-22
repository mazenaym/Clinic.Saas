using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class DashboardStatsDto
    {
        public int TodayAppointments { get; set; }
        public int TodayCompletedVisits { get; set; }
        public decimal TodayRevenue { get; set; }
        public int PendingAppointments { get; set; }
        public int TotalPatientsThisMonth { get; set; }
        public int NewPatientsThisMonth { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<LowInventoryAlertDto> LowStockItems { get; set; } = new();
    }
}
