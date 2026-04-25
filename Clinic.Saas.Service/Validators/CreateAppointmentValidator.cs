using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.Validators;

public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentDto>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("المريض مطلوب");
        RuleFor(x => x.DoctorId).NotEmpty().WithMessage("الدكتور مطلوب");
        RuleFor(x => x.AppointmentDate).GreaterThanOrEqualTo(DateTime.Today).WithMessage("تاريخ الموعد غير صحيح");
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithMessage("وقت النهاية يجب أن يكون بعد وقت البداية");
        RuleFor(x => x.Type).IsInEnum().WithMessage("نوع الموعد غير صحيح");
        RuleFor(x => x.Source).IsInEnum().WithMessage("مصدر الموعد غير صحيح");
    }
}
