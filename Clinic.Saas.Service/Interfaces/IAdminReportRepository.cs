using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.Interfaces;

public interface IAdminReportRepository
{
    Task<IEnumerable<ClinicUsageMetricDto>> GetClinicUsageMetricsAsync();
    Task<IEnumerable<SubscriptionRevenueDto>> GetSubscriptionRevenueAsync();
    Task<IEnumerable<ExpiringSubscriptionDto>> GetExpiringSubscriptionsAsync(int days);
    Task<IEnumerable<AuditLogDto>> GetActivityLogAsync(int take, Guid? tenantId);
}
