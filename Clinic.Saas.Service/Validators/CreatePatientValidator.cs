using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation; 
using Clinic.Saas.Service.DTOs;
using AutoMapper;

namespace Clinic.Saas.Service.Validators
{
    public class CreatePatientValidator : AbstractValidator<CreatePatientDto>
    {
        public CreatePatientValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("الاسم مطلوب")
                .MinimumLength(3).WithMessage("الاسم لازم يكون على الأقل 3 أحرف")
                .MaximumLength(200).WithMessage("الاسم طويل جداً");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("رقم الموبايل مطلوب")
                .Matches(@"^01[0-2,5]{1}[0-9]{8}$")
                .WithMessage("رقم الموبايل غير صحيح");

            RuleFor(x => x.Gender)
                .IsInEnum().WithMessage("النوع غير صحيح");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("البريد الإلكتروني غير صحيح")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.Now).WithMessage("تاريخ الميلاد غير صحيح")
                .When(x => x.DateOfBirth.HasValue);
        }
    }
}
