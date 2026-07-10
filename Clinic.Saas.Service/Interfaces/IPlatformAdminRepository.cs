using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.Interfaces;

public interface IPlatformAdminRepository
{
    Task<AdminDashboardStatsDto> GetDashboardStatsAsync(DateTime utcNow);
    Task<IEnumerable<AdminClinicDto>> GetClinicsAsync();
    Task<AdminClinicDto?> GetClinicByIdAsync(Guid clinicId);
    Task<bool> SubdomainExistsAsync(string subdomain, Guid? excludeTenantId = null);
    Task<bool> SuperAdminExistsAsync();
    Task<AdminClinicDto?> BootstrapSuperAdminAsync(Tenant platformTenant, User superAdmin);
    Task<AdminClinicDto> CreateClinicAsync(Tenant tenant, User owner, Subscription subscription, ClinicSettingsDto settings);
    Task UpdateClinicAsync(Tenant tenant);
    Task SetClinicStatusAsync(Guid clinicId, bool isActive);
    Task<Subscription> AddSubscriptionAsync(Subscription subscription);
}
