using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.Validators;

public class RegisterClinicValidator : AbstractValidator<RegisterClinicDto>
{
    public RegisterClinicValidator()
    {
        RuleFor(x => x.ClinicName).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .Matches("^[a-zA-Z0-9-]+$")
            .MinimumLength(3)
            .MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.OwnerFullName).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.OwnerEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.OwnerPassword).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Plan).IsInEnum();
        RuleFor(x => x.TimeZone).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.CloseTime).GreaterThan(x => x.OpenTime);
        RuleFor(x => x.SlotDurationMin).InclusiveBetween(5, 240);
        RuleFor(x => x.ConsultFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxPct).InclusiveBetween(0, 100);
    }
}
