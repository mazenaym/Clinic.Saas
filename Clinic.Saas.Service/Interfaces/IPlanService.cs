using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Service.Interfaces;

public interface IPlanService
{
    Task<IReadOnlyList<PlatformPlanDto>> GetActivePlansAsync();
    Task<IReadOnlyList<PlatformPlanDto>> GetPlansAsync(bool includeInactive = true);
    Task<PlatformPlanDto?> GetPlanByIdAsync(Guid id);
    Task<PlatformPlanDto?> GetPlanByCodeAsync(string code);
    Task<PlatformPlanDto> CreatePlanAsync(UpsertPlatformPlanDto dto);
    Task<PlatformPlanDto?> UpdatePlanAsync(Guid id, UpsertPlatformPlanDto dto);
    Task<DeletePlanResult> DeletePlanAsync(Guid id);
    Task<bool> SetPlanActiveAsync(Guid id, bool isActive);
}
