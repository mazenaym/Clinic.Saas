using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.Validators;

public class CreatePrescriptionValidator : AbstractValidator<CreatePrescriptionDto>
{
    public CreatePrescriptionValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty().WithMessage("الكشف مطلوب");
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("المريض مطلوب");
        RuleFor(x => x.DoctorId).NotEmpty().WithMessage("الدكتور مطلوب");
        RuleFor(x => x.Items).NotEmpty().WithMessage("يجب إضافة دواء واحد على الأقل");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.DrugName).NotEmpty().WithMessage("اسم الدواء مطلوب");
            item.RuleFor(x => x.Dosage).NotEmpty().WithMessage("الجرعة مطلوبة");
            item.RuleFor(x => x.Frequency).NotEmpty().WithMessage("عدد المرات مطلوب");
            item.RuleFor(x => x.Duration).NotEmpty().WithMessage("المدة مطلوبة");
        });
    }
}
