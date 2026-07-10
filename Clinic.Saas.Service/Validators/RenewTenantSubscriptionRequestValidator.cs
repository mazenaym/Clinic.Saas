using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.Validators;

public sealed class RenewTenantSubscriptionRequestValidator : AbstractValidator<RenewTenantSubscriptionRequest>
{
    public RenewTenantSubscriptionRequestValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.PlanId).NotEmpty().WithMessage("PlanId is required.");
        RuleFor(x => x.ActualPaidAmount).GreaterThanOrEqualTo(0).When(x => x.ActualPaidAmount.HasValue).WithMessage("ActualPaidAmount must be greater than or equal to 0.");
        RuleFor(x => x.PaymentMethod).MaximumLength(100).WithMessage("PaymentMethod must be 100 characters or fewer.");
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("Notes must be 500 characters or fewer.");
    }
}
