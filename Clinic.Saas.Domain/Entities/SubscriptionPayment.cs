using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Domain.Entities;

public class SubscriptionPayment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public SubscriptionPaymentStatus PaymentStatus { get; set; } = SubscriptionPaymentStatus.Pending;
    public string? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
