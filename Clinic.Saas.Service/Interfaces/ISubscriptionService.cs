using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.Interfaces;

public interface ISubscriptionService
{
    Task<TenantSubscriptionDto> CreateInitialSubscriptionAsync(Guid tenantId, Guid? planId, Guid? createdByUserId = null);
    Task<TenantSubscriptionDto?> GetCurrentSubscriptionAsync(Guid tenantId);
    Task<TenantSubscriptionDto?> GetSubscriptionByIdAsync(Guid id);
    Task<SubscriptionStatusDto> GetSubscriptionStatusAsync(Guid tenantId);
    Task<TenantSubscriptionDto?> RenewSubscriptionAsync(RenewTenantSubscriptionRequest request, Guid? renewedByUserId);
    Task<TenantSubscriptionDto?> ChangeSubscriptionPlanAsync(RenewTenantSubscriptionRequest request, Guid? changedByUserId);
    Task<bool> SuspendTenantAsync(Guid tenantId, string reason, Guid? suspendedByUserId);
    Task<bool> DisableTenantAsync(Guid tenantId, string reason, Guid? disabledByUserId);
    Task<bool> ReactivateTenantAsync(Guid tenantId, Guid? reactivatedByUserId);
    Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string? reason, Guid? cancelledByUserId);
    Task<SubscriptionExpiryResultDto> CheckAndExpireSubscriptionsAsync();
    Task<IReadOnlyList<TenantSubscriptionDto>> GetExpiringSoonSubscriptionsAsync(int days);
    Task<IReadOnlyList<TenantSubscriptionDto>> GetExpiredSubscriptionsAsync();
    Task<IReadOnlyList<TenantSubscriptionDto>> GetSubscriptionsAsync(PlatformSubscriptionFilterDto filter);
}
