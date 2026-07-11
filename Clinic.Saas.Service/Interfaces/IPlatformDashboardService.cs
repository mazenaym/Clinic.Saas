using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.Interfaces;

public interface IPlatformDashboardService
{
    Task<PlatformDashboardSummaryDto> GetDashboardSummaryAsync();
    Task<IReadOnlyList<AdminClinicDto>> GetClinicsOverviewAsync(PlatformClinicFilterDto filter);
    Task<IReadOnlyList<AdminClinicDto>> GetRecentClinicsAsync(int take = 5);
    Task<PlatformReportsDto> GetReportsAsync(PlatformReportsFilterDto filter);
    Task<PlatformRevenueAnalyticsDto> GetRevenueAnalyticsAsync(PlatformRevenueAnalyticsFilterDto filter);
    Task<PlatformSettingsDto> GetSettingsAsync();
    Task<PlatformSettingsDto> UpdateSettingsAsync(PlatformSettingsDto settings, Guid? updatedByUserId);
}
