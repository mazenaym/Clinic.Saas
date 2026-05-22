using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.Validators;

public class UpdateClinicValidator : AbstractValidator<UpdateClinicDto>
{
    public UpdateClinicValidator()
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
    }
}
