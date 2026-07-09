using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.Jobs;

public class SubscriptionExpiryRecurringJob
{
    private readonly ISubscriptionService _subscriptions;

    public SubscriptionExpiryRecurringJob(ISubscriptionService subscriptions)
    {
        _subscriptions = subscriptions;
    }

    public Task<SubscriptionExpiryResultDto> RunAsync()
    {
        return _subscriptions.CheckAndExpireSubscriptionsAsync();
    }
}
