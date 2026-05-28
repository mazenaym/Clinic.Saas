using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Exceptions;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Payments.Commands;

public class UpdatePaymentCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid PaymentId { get; set; }
        public UpdatePaymentDto Payment { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IPaymentRepository _repository;

        public Handler(IPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<object>> Handle(Command command)
        {
            var existing = await _repository.GetByIdAsync(command.TenantId, command.PaymentId);
            if (existing is null)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "Payment not found.",
                    StatusCode = 404
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
                Id = command.PaymentId,
                TenantId = command.TenantId,
                VisitId = command.Payment.VisitId,
                PatientId = command.Payment.PatientId,
                TotalAmount = command.Payment.TotalAmount,
                DiscountAmount = command.Payment.DiscountAmount,
                DiscountPct = command.Payment.DiscountPct,
                TaxAmount = command.Payment.TaxAmount,
                PaidAmount = command.Payment.PaidAmount,
                PaymentMethod = command.Payment.PaymentMethod,
                Status = status,
                InsuranceCompany = command.Payment.InsuranceCompany,
                InsuranceNumber = command.Payment.InsuranceNumber,
                Notes = command.Payment.Notes,
                RowVersion = string.IsNullOrWhiteSpace(command.Payment.RowVersion)
                    ? existing.RowVersion
                    : command.Payment.RowVersion.FromBase64RowVersion(),
                Items = command.Payment.Items.Select(item => new PaymentItem
                {
                    ServiceName = item.ServiceName,
                    ServiceType = item.ServiceType,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPct = item.DiscountPct
                }).ToList()
            };

            bool updated;
            try
            {
                updated = await _repository.UpdateWithItemsAsync(command.TenantId, entity);
            }
            catch (ConcurrencyConflictException ex)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 409
                };
            }
            catch (RecordNotFoundException ex)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                };
            }
            if (!updated)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "Payment not found.",
                    StatusCode = 404
                };
            }

            return new BaseResponse<object>
            {
                Success = true,
                Message = "Payment updated.",
                Data = true,
                StatusCode = 200
            };
        }
    }
}
