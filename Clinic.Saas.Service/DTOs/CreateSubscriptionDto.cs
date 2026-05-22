using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Service.DTOs;

public class CreateSubscriptionDto
{
    public PlanType Plan { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal AmountPaid { get; set; }
    public SubscriptionStatus Status { get; set; }
    public string? PaymentRef { get; set; }
    public string? Notes { get; set; }
}
