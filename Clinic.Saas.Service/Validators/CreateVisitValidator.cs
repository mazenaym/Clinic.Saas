using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.Validators;

public class CreateVisitValidator : AbstractValidator<CreateVisitDto>
{
    public CreateVisitValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("المريض مطلوب");
        RuleFor(x => x.DoctorId).NotEmpty().WithMessage("الدكتور مطلوب");
        RuleFor(x => x.ChiefComplaint).NotEmpty().WithMessage("الشكوى الرئيسية مطلوبة");
        RuleFor(x => x.VisitType).IsInEnum().WithMessage("نوع الزيارة غير صحيح");
    }
}
