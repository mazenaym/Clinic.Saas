using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Domain.Entities;

public class InvoicePayment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
    public DateTime PaidAt { get; set; }
    public Guid? CreatedBy { get; set; }
}
