using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using Clinic.Saas.Service.DTOs;
namespace Clinic.Saas.Service.Validators
{
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                .WithMessage("Current password is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("New password is required.")
                .MinimumLength(6)
                .WithMessage("New password must be at least 6 characters.")
                .NotEqual(x => x.CurrentPassword)
                .WithMessage("New password must be different from current password.");
        }
    }
}
