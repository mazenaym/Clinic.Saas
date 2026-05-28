using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? RowVersion { get; set; }
    }
}
