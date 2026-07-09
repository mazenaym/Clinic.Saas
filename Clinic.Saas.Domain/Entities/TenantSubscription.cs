using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Domain.Entities;

public class TenantSubscription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public DateTime? RenewedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime? SuspendedAtUtc { get; set; }
    public bool AutoRenew { get; set; }
    public int GracePeriodDays { get; set; }
    public DateTime? LastCheckedAtUtc { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
