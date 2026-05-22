using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.Validators;

public class CreateClinicValidator : AbstractValidator<CreateClinicDto>
{
    public CreateClinicValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .Matches("^[a-zA-Z0-9-]+$")
            .MinimumLength(3)
            .MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Plan).IsInEnum();
        RuleFor(x => x.TimeZone).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.OwnerFullName).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.OwnerEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.OwnerPassword).NotEmpty().MinimumLength(6);
        RuleFor(x => x.SubscriptionAmountPaid).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SubscriptionStatus).IsInEnum();
        RuleFor(x => x.SubscriptionEndDate)
            .GreaterThan(x => x.SubscriptionStartDate)
            .When(x => x.SubscriptionStartDate.HasValue && x.SubscriptionEndDate.HasValue);
        RuleFor(x => x.CloseTime).GreaterThan(x => x.OpenTime);
        RuleFor(x => x.SlotDurationMin).InclusiveBetween(5, 240);
        RuleFor(x => x.ConsultFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxPct).InclusiveBetween(0, 100);
    }
}
