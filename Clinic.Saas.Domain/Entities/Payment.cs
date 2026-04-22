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
        public decimal RemainingAmount { get; private set; } // Computed
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public string? InsuranceCompany { get; set; }
        public string? InsuranceNumber { get; set; }
        public string? ReceiptUrl { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }

        // Navigation Properties
        public Tenant Tenant { get; set; } = null!;
        public Visit Visit { get; set; } = null!;
        public Patient Patient { get; set; } = null!;
        public ICollection<PaymentItem> Items { get; set; } = new List<PaymentItem>();
    }
}
