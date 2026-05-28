using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Invoices;

namespace Clinic.Saas.Service.UseCases.Invoices.Commands;

public class AddInvoicePaymentCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid InvoiceId { get; set; }
        public Guid? UserId { get; set; }
        public AddInvoicePaymentDto Payment { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public Handler(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public async Task<BaseResponse<InvoiceDto>> Handle(Command command)
        {
            if (command.Payment.Amount <= 0)
            {
                return new BaseResponse<InvoiceDto>
                {
                    Success = false,
                    Message = "Payment amount must be positive.",
                    Errors = ["Payment amount must be positive."],
                    StatusCode = 400
                };
            }

            try
            {
                var invoice = await _invoiceRepository.AddPaymentAsync(command.TenantId, command.InvoiceId, new InvoicePayment
                {
                    Amount = command.Payment.Amount,
                    PaymentMethod = command.Payment.PaymentMethod,
                    PaymentReference = command.Payment.PaymentReference,
                    Notes = command.Payment.Notes,
                    CreatedBy = command.UserId
                });

                if (invoice is null)
                {
                    return new BaseResponse<InvoiceDto>
                    {
                        Success = false,
                        Message = "Invoice not found.",
                        StatusCode = 404
                    };
                }

                return new BaseResponse<InvoiceDto>
                {
                    Success = true,
                    Message = "Invoice payment added.",
                    StatusCode = 200,
                    Data = InvoiceMapper.ToDto(invoice)
                };
            }
            catch (InvalidOperationException ex)
            {
                return new BaseResponse<InvoiceDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = [ex.Message],
                    StatusCode = 409
                };
            }
        }
    }
}
