using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.Validators;

public class CreatePaymentValidator : AbstractValidator<CreatePaymentDto>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty().WithMessage("الكشف مطلوب");
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("المريض مطلوب");
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0).WithMessage("إجمالي الفاتورة غير صحيح");
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0).WithMessage("المبلغ المدفوع غير صحيح");
        RuleFor(x => x.PaymentMethod).IsInEnum().WithMessage("طريقة الدفع غير صحيحة");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ServiceName).NotEmpty().WithMessage("اسم الخدمة مطلوب");
            item.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("سعر الخدمة غير صحيح");
        });
    }
}
