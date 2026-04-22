using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class PaymentDetailsDto : PaymentDto
    {
        public decimal DiscountPct { get; set; }
        public decimal TaxAmount { get; set; }
        public string? InsuranceCompany { get; set; }
        public string? Notes { get; set; }
        public List<PaymentItemDto> Items { get; set; } = new();
    }
}
