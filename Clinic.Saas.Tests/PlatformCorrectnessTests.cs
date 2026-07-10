using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Validators;

namespace Clinic.Saas.Tests;

public sealed class PlatformCorrectnessTests
{
    [Fact]
    public async Task Renewal_validator_covers_all_platform_rules()
    {
        var validator = new RenewTenantSubscriptionRequestValidator();
        var result = await validator.ValidateAsync(new RenewTenantSubscriptionRequest(
            Guid.Empty, Guid.Empty, null, -1, null, new string('x', 101), new string('n', 501)));

        Assert.Contains(result.Errors, x => x.ErrorMessage == "TenantId is required.");
        Assert.Contains(result.Errors, x => x.ErrorMessage == "PlanId is required.");
        Assert.Contains(result.Errors, x => x.ErrorMessage == "ActualPaidAmount must be greater than or equal to 0.");
        Assert.Contains(result.Errors, x => x.ErrorMessage == "PaymentMethod must be 100 characters or fewer.");
        Assert.Contains(result.Errors, x => x.ErrorMessage == "Notes must be 500 characters or fewer.");
    }

    [Fact]
    public async Task Renewal_validator_accepts_null_amount_and_boundary_lengths()
    {
        var validator = new RenewTenantSubscriptionRequestValidator();
        var result = await validator.ValidateAsync(new RenewTenantSubscriptionRequest(
            Guid.NewGuid(), Guid.NewGuid(), null, null, null, new string('x', 100), new string('n', 500)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Subscription_service_exposes_direct_lookup_and_distinct_change_plan_operations()
    {
        var service = typeof(Clinic.Saas.Service.Interfaces.ISubscriptionService);
        Assert.NotNull(service.GetMethod("GetSubscriptionByIdAsync"));
        Assert.NotNull(service.GetMethod("ChangeSubscriptionPlanAsync"));
    }
}
