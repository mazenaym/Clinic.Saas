using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Service.DTOs;

public class AdminClinicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? LogoUrl { get; set; }
    public PlanType Plan { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int UsersCount { get; set; }
    public int PatientsCount { get; set; }
    public int AppointmentsCount { get; set; }
    public decimal ClinicRevenue { get; set; }
    public Guid? SubscriptionId { get; set; }
    public SubscriptionStatus? SubscriptionStatus { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public decimal? SubscriptionAmountPaid { get; set; }
}
