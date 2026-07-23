using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Service.DTOs;

public record CreateSubscriptionRequest
{
    public Guid TenantId { get; init; }
    public PlanType Plan { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal AmountPaid { get; init; }
    public SubscriptionStatus Status { get; init; }
    public string? PaymentRef { get; init; }
    public string? Notes { get; init; }
}
