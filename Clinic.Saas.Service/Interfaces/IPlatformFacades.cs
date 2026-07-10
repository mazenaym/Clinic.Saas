using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.Interfaces;

public interface IPlatformDashboardFacade
{
    Task<PlatformDashboardSummaryDto> GetSummaryAsync();
    Task<BaseResponse<AdminDashboardStatsDto>> GetLegacySummaryAsync();
}

public interface IPlatformClinicsFacade
{
    Task<IReadOnlyList<AdminClinicDto>> GetOverviewAsync(PlatformClinicFilterDto filter);
    Task<BaseResponse<IEnumerable<AdminClinicDto>>> GetLegacyListAsync();
    Task<BaseResponse<AdminClinicDto>> GetAsync(Guid id);
    Task<BaseResponse<AdminClinicDto>> CreateAsync(CreateClinicDto dto);
    Task<BaseResponse<AdminClinicDto>> UpdateAsync(Guid id, UpdateClinicDto dto);
    Task<BaseResponse<AdminClinicDto>> SetLegacyStatusAsync(Guid id, bool isActive);
    Task<BaseResponse<Subscription>> CreateLegacySubscriptionAsync(Guid clinicId, CreateSubscriptionDto dto);
    Task<BaseResponse<AdminClinicDto>> BootstrapAsync(BootstrapSuperAdminDto dto);
}

public interface IPlatformPlansFacade
{
    Task<IReadOnlyList<PlatformPlanDto>> GetAsync(bool includeInactive);
    Task<PlatformPlanDto?> GetAsync(Guid id);
    Task<PlatformPlanDto> CreateAsync(UpsertPlatformPlanDto dto);
    Task<PlatformPlanDto?> UpdateAsync(Guid id, UpsertPlatformPlanDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> SetActiveAsync(Guid id, bool active);
    Task<BaseResponse<PlatformPlanDto>> CreateLegacyAsync(CreatePlatformPlanRequest request);
    Task<BaseResponse<PlatformPlanDto?>> UpdateLegacyAsync(Guid id, UpdatePlatformPlanRequest request);
    Task<BaseResponse<bool>> DeleteLegacyAsync(Guid id);
    Task<BaseResponse<bool>> SetLegacyStatusAsync(Guid id, UpdatePlatformPlanStatusRequest request, string successMessage);
}

public interface IPlatformReportsFacade
{
    Task<PlatformDashboardSummaryDto> GetRevenueAsync();
    Task<IReadOnlyList<TenantSubscriptionDto>> GetSubscriptionsAsync(PlatformSubscriptionFilterDto filter);
    Task<IReadOnlyList<AdminClinicDto>> GetClinicsAsync(PlatformClinicFilterDto filter);
    Task<PlatformReportsDto> GetPlatformAsync(PlatformReportsFilterDto filter);
    Task<BaseResponse<List<ClinicUsageMetricDto>>> GetLegacyUsageAsync();
    Task<BaseResponse<List<SubscriptionRevenueDto>>> GetLegacyRevenueAsync();
    Task<BaseResponse<List<ExpiringSubscriptionDto>>> GetLegacyExpiringAsync(int days);
}

public interface IPlatformAuditLogsFacade
{
    Task<BaseResponse<List<AuditLogDto>>> GetAsync(int take, bool includeAllTenants, Guid? tenantId);
}
