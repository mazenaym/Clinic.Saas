using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.Validators;

public class BootstrapSuperAdminValidator : AbstractValidator<BootstrapSuperAdminDto>
{
    public BootstrapSuperAdminValidator()
    {
        RuleFor(x => x.PlatformName).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.PlatformSubdomain)
            .NotEmpty()
            .Matches("^[a-zA-Z0-9-]+$")
            .MinimumLength(3)
            .MaximumLength(100);
        RuleFor(x => x.PlatformEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
