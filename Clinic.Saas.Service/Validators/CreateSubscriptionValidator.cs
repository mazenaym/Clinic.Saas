using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.Validators;

public class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionDto>
{
    public CreateSubscriptionValidator()
    {
        RuleFor(x => x.Plan).IsInEnum();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
        RuleFor(x => x.AmountPaid).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status).IsInEnum();
    }
}
