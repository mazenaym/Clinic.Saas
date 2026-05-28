using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Invoices;

namespace Clinic.Saas.Service.UseCases.Invoices.Commands;

public class CreateInvoiceCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid? UserId { get; set; }
        public CreateInvoiceDto Invoice { get; set; } = null!;
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
            if (command.Invoice.PatientId == Guid.Empty)
            {
                return Fail("PatientId is required.", 400);
            }

            if (command.Invoice.Items.Count == 0)
            {
                return Fail("Invoice must include at least one item.", 400);
            }

            var items = new List<InvoiceItem>();
            var sortOrder = 0;
            foreach (var item in command.Invoice.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Description))
                {
                    return Fail("Invoice item description is required.", 400);
                }

                if (item.Quantity <= 0 || item.UnitPrice < 0 || item.DiscountAmount < 0 || item.TaxAmount < 0)
                {
                    return Fail("Invoice item amounts are invalid.", 400);
                }

                var gross = item.Quantity * item.UnitPrice;
                var lineTotal = gross - item.DiscountAmount + item.TaxAmount;
                if (lineTotal < 0)
                {
                    return Fail("Invoice item total cannot be negative.", 400);
                }

                items.Add(new InvoiceItem
                {
                    ProcedureId = item.ProcedureId,
                    Description = item.Description.Trim(),
                    ServiceType = item.ServiceType,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    TaxAmount = item.TaxAmount,
                    LineTotal = lineTotal,
                    SortOrder = sortOrder++
                });
            }

            var subtotal = items.Sum(x => x.Quantity * x.UnitPrice);
            var discount = items.Sum(x => x.DiscountAmount);
            var tax = items.Sum(x => x.TaxAmount);
            var grandTotal = subtotal - discount + tax;

            var invoice = await _invoiceRepository.AddAsync(new Invoice
            {
                TenantId = command.TenantId,
                PatientId = command.Invoice.PatientId,
                VisitId = command.Invoice.VisitId,
                Subtotal = subtotal,
                DiscountAmount = discount,
                TaxAmount = tax,
                GrandTotal = grandTotal,
                PaidAmount = 0,
                RemainingAmount = grandTotal,
                Status = InvoiceStatus.Draft,
                Notes = command.Invoice.Notes,
                CreatedBy = command.UserId,
                Items = items
            });

            return new BaseResponse<InvoiceDto>
            {
                Success = true,
                Message = "Invoice created.",
                StatusCode = 200,
                Data = InvoiceMapper.ToDto(invoice)
            };
        }

        private static BaseResponse<InvoiceDto> Fail(string message, int statusCode) => new()
        {
            Success = false,
            Message = message,
            Errors = [message],
            StatusCode = statusCode
        };
    }
}
