namespace Clinic.Saas.Service.DTOs;

public class BootstrapSuperAdminDto
{
    public string PlatformName { get; set; } = "Clinic Flow Platform";
    public string PlatformSubdomain { get; set; } = "platform";
    public string PlatformEmail { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
