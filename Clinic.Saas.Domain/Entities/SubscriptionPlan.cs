namespace Clinic.Saas.Domain.Entities;

public class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EGP";
    public int DurationDays { get; set; }
    public int? MaxUsers { get; set; }
    public int? MaxPatients { get; set; }
    public int? MaxDoctors { get; set; }
    public string? FeaturesJson { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
