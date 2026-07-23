using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.Interfaces;

public interface IPlatformDashboardFacade
{
    Task<PlatformDashboardSummaryDto> GetSummaryAsync();
}

public interface IPlatformClinicsFacade
{
    Task<IReadOnlyList<AdminClinicDto>> GetAsync(PlatformClinicFilterDto filter);
    Task<BaseResponse<AdminClinicDto>> GetByIdAsync(Guid id);
    Task<BaseResponse<AdminClinicDto>> CreateWithInitialSubscriptionAsync(CreateClinicDto dto, Guid? planId, Guid? actingUserId);
    Task<BaseResponse<AdminClinicDto>> CreateAsync(CreateClinicDto dto);
    Task<BaseResponse<AdminClinicDto>> UpdateAsync(Guid id, UpdateClinicDto dto);
    Task<BaseResponse<AdminClinicDto>> SetLegacyStatusAsync(Guid id, bool isActive);
    Task<BaseResponse<AdminClinicDto>> CreateLegacySubscriptionAsync(Guid clinicId, CreateSubscriptionDto dto);
    Task<BaseResponse<AdminClinicDto>> BootstrapAsync(BootstrapSuperAdminDto dto);
}

public interface IPlatformPlansFacade
{
    Task<IReadOnlyList<PlatformPlanDto>> GetAsync(bool includeInactive);
    Task<PlatformPlanDto?> GetByIdAsync(Guid id);
    Task<PlatformPlanDto?> GetByCodeAsync(string code);
    Task<PlatformPlanDto> CreateAsync(UpsertPlatformPlanDto dto, Guid? actingUserId);
    Task<PlatformPlanDto?> UpdateAsync(Guid id, UpsertPlatformPlanDto dto, Guid? actingUserId);
    Task<DeletePlanResult> DeleteAsync(Guid id, Guid? actingUserId);
    Task<bool> SetActiveAsync(Guid id, bool active, Guid? actingUserId);
}

public interface IPlatformReportsFacade
{
    Task<PlatformDashboardSummaryDto> GetRevenueAsync();
    Task<IReadOnlyList<TenantSubscriptionDto>> GetSubscriptionsAsync(PlatformSubscriptionFilterDto filter);
    Task<IReadOnlyList<AdminClinicDto>> GetClinicsAsync(PlatformClinicFilterDto filter);
    Task<PlatformReportsDto> GetPlatformAsync(PlatformReportsFilterDto filter);
}

public interface IPlatformSubscriptionsFacade
{
    Task<TenantSubscriptionDto?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<TenantSubscriptionDto>> GetExpiringAsync(int days);
    Task<BaseResponse<TenantSubscriptionDto>> RenewAsync(Guid tenantId, RenewTenantSubscriptionRequest request, Guid? actingUserId);
    Task<BaseResponse<TenantSubscriptionDto>> ChangePlanAsync(Guid tenantId, RenewTenantSubscriptionRequest request, Guid? actingUserId);
}

public interface IPlatformAuditLogsFacade
{
    Task<BaseResponse<List<AuditLogDto>>> GetAsync(int take, bool includeAllTenants, Guid? tenantId);
}
