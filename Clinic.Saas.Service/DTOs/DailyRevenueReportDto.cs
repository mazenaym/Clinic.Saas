using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class DailyRevenueReportDto
    {
        public DateTime Date { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedVisits { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal CashPayments { get; set; }
        public decimal CardPayments { get; set; }
        public decimal InsurancePayments { get; set; }
    }
}
