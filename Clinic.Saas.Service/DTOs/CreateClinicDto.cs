using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Service.DTOs;

public class CreateClinicDto
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? LogoUrl { get; set; }
    public PlanType Plan { get; set; } = PlanType.Starter;
    public string TimeZone { get; set; } = "Africa/Cairo";
    public string Currency { get; set; } = "EGP";
    public string OwnerFullName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerPassword { get; set; } = string.Empty;
    public string? OwnerPhone { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public decimal SubscriptionAmountPaid { get; set; }
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Trial;
    public string? PaymentRef { get; set; }
    public string? Notes { get; set; }
    public TimeSpan OpenTime { get; set; } = new(9, 0, 0);
    public TimeSpan CloseTime { get; set; } = new(21, 0, 0);
    public int SlotDurationMin { get; set; } = 20;
    public decimal ConsultFee { get; set; }
    public decimal TaxPct { get; set; }
}
