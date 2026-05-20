using AutoMapper;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.UseCases.Payments.Commands;

public class CreatePaymentCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public CreatePaymentDto Payment { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IPaymentRepository _repository;
        private readonly IValidator<CreatePaymentDto> _validator;
        private readonly IMapper _mapper;

        public Handler(IPaymentRepository repository, IValidator<CreatePaymentDto> validator, IMapper mapper)
        {
            _repository = repository;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<BaseResponse<PaymentDto>> Handle(Command command)
        {
            var validation = await _validator.ValidateAsync(command.Payment);
            if (!validation.IsValid)
            {
                return new BaseResponse<PaymentDto>
                {
                    Success = false,
                    Message = "بيانات الفاتورة غير صحيحة",
                    Errors = validation.Errors.Select(x => x.ErrorMessage).ToList(),
                    StatusCode = 400
                };
            }

            var netAmount = command.Payment.TotalAmount + command.Payment.TaxAmount - command.Payment.DiscountAmount;
            var status = command.Payment.PaidAmount switch
            {
                <= 0 => PaymentStatus.Pending,
                var paid when paid < netAmount => PaymentStatus.Partial,
                _ => PaymentStatus.Paid
            };

            var entity = new Payment
            {
                TenantId = command.TenantId,
                VisitId = command.Payment.VisitId,
                PatientId = command.Payment.PatientId,
                TotalAmount = command.Payment.TotalAmount,
                DiscountAmount = command.Payment.DiscountAmount,
                DiscountPct = command.Payment.DiscountPct,
                TaxAmount = command.Payment.TaxAmount,
                PaidAmount = command.Payment.PaidAmount,
                RemainingAmount = netAmount - command.Payment.PaidAmount,
                PaymentMethod = command.Payment.PaymentMethod,
                Status = status,
                InsuranceCompany = command.Payment.InsuranceCompany,
                InsuranceNumber = command.Payment.InsuranceNumber,
                Notes = command.Payment.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = command.Payment.Items.Select(item => new PaymentItem
                {
                    ServiceName = item.ServiceName,
                    ServiceType = item.ServiceType,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPct = item.DiscountPct,
                    TotalPrice = (item.Quantity * item.UnitPrice) * (1 - (item.DiscountPct / 100m))
                }).ToList()
            };

            var created = await _repository.AddAsync(entity);
            return new BaseResponse<PaymentDto>
            {
                Success = true,
                Message = "تم إنشاء الفاتورة بنجاح",
                Data = _mapper.Map<PaymentDto>(created),
                StatusCode = 201
            };
        }
    }
}
