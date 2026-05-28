using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.Interfaces;

public interface IOperationsTenantRepository
{
    Task<TenantSubscriptionStatusDto?> GetTenantStatusAsync(Guid tenantId);
    Task<UpdateClinicSettingsDto> GetSettingsAsync(Guid tenantId);
    Task UpsertSettingsAsync(Guid tenantId, UpdateClinicSettingsDto settings);
}
