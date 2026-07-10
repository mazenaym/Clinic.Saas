using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.Validators;

public class CreatePaymentValidator : AbstractValidator<CreatePaymentDto>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty().WithMessage("اختر الكشف المرتبط بالفاتورة.");
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("اختر المريض.");
        RuleFor(x => x).Must(x => CalculateItemsTotal(x) > 0)
            .WithMessage("إجمالي الفاتورة يجب أن يكون أكبر من صفر.");
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("قيمة الخصم غير صحيحة.");
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).WithMessage("قيمة الضريبة غير صحيحة.");
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0).WithMessage("المبلغ المدفوع غير صحيح.");
        RuleFor(x => x.PaymentMethod).IsInEnum().WithMessage("طريقة الدفع غير صحيحة.");
        RuleFor(x => x).Must(x => x.DiscountAmount <= CalculateItemsTotal(x) + x.TaxAmount)
            .WithMessage("قيمة الخصم لا يمكن أن تتجاوز إجمالي الفاتورة.");
        RuleFor(x => x).Must(x => x.PaidAmount <= CalculateItemsTotal(x) + x.TaxAmount - x.DiscountAmount)
            .WithMessage("المبلغ المدفوع لا يمكن أن يتجاوز المبلغ المستحق.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("أضف خدمة واحدة على الأقل.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ServiceName).NotEmpty().WithMessage("اسم الخدمة مطلوب");
            item.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("سعر الخدمة غير صحيح");
            item.RuleFor(x => x.DiscountPct).InclusiveBetween(0, 100).WithMessage("نسبة خصم الخدمة يجب أن تكون بين 0 و100.");
        });
    }

    private static decimal CalculateItemsTotal(CreatePaymentDto payment) =>
        payment.Items.Sum(item => decimal.Round(
            (item.Quantity * item.UnitPrice) * (1 - (item.DiscountPct / 100m)),
            2));
}
