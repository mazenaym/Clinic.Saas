using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IPlatformPlanRepository
{
    Task<IReadOnlyList<SubscriptionPlan>> GetAllAsync(bool includeInactive = true);
    Task<SubscriptionPlan?> GetByIdAsync(Guid id);
    Task<SubscriptionPlan?> GetByCodeAsync(string code);
    Task<SubscriptionPlan> CreateAsync(SubscriptionPlan plan);
    Task<SubscriptionPlan?> UpdateAsync(SubscriptionPlan plan);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> UpdateStatusAsync(Guid id, bool isActive);
}
