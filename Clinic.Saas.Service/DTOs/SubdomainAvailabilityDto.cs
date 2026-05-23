namespace Clinic.Saas.Service.DTOs;

public class SubdomainAvailabilityDto
{
    public string Subdomain { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string? Reason { get; set; }
}
