using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class CreatePaymentItemDto
    {
        public string ServiceName { get; set; } = string.Empty;
        public ServiceType ServiceType { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal DiscountPct { get; set; }
    }
}
