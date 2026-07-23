using Clinic.Saas.Domain.Exceptions;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Invoices.Commands;

public class RefundInvoiceCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid InvoiceId { get; set; }
        public RefundInvoiceDto Refund { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IInvoiceRepository _repository;

        public Handler(IInvoiceRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<object>> Handle(Command command)
        {
            var existing = await _repository.GetByIdAsync(command.TenantId, command.InvoiceId);
            if (existing is null)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "Invoice not found.",
                    StatusCode = 404
                };
            }

            bool refunded;
            try
            {
                var rowVersion = string.IsNullOrWhiteSpace(command.Refund.RowVersion)
                    ? existing.RowVersion
                    : command.Refund.RowVersion.FromBase64RowVersion();

                refunded = await _repository.RefundAsync(
                    command.TenantId,
                    command.InvoiceId,
                    command.Refund.Reason,
                    rowVersion);
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

            if (!refunded)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "Invoice not found.",
                    StatusCode = 404
                };
            }

            return new BaseResponse<object>
            {
                Success = true,
                Message = "Invoice refunded.",
                Data = true,
                StatusCode = 200
            };
        }
    }
}
