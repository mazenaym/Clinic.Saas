using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Service.DTOs;

public class UpdateClinicDto
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? LogoUrl { get; set; }
    public PlanType Plan { get; set; }
    public string TimeZone { get; set; } = "Africa/Cairo";
    public string Currency { get; set; } = "EGP";
    public bool IsActive { get; set; }
}
