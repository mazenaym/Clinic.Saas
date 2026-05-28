using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid VisitId { get; set; }
        public Guid PatientId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPct { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public string? InsuranceCompany { get; set; }
        public string? InsuranceNumber { get; set; }
        public string? ReceiptUrl { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }

        public ICollection<PaymentItem> Items { get; set; } = new List<PaymentItem>();
        public string PatientName { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; } = [];
    }
}
