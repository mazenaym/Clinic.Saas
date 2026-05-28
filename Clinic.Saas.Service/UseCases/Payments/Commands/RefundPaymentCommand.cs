using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Payments.Commands;

public class RefundPaymentCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid PaymentId { get; set; }
        public RefundPaymentDto Refund { get; set; } = null!;
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
            var refunded = await _repository.RefundAsync(
                command.TenantId,
                command.PaymentId,
                command.Refund.Reason);

            if (!refunded)
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
                Message = "Payment refunded.",
                Data = true,
                StatusCode = 200
            };
        }
    }
}
