using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Exceptions;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Invoices.Commands;

public class UpdateInvoiceCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid InvoiceId { get; set; }
        public UpdateInvoiceDto Invoice { get; set; } = null!;
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

            var items = command.Invoice.Items.Select(item => new InvoiceItem
            {
                ProcedureId = item.ProcedureId,
                Description = item.Description,
                ServiceType = item.ServiceType,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TaxAmount = item.TaxAmount,
                LineTotal = (item.Quantity * item.UnitPrice) - item.DiscountAmount + item.TaxAmount,
                SortOrder = item.SortOrder
            }).ToList();

            var subtotal = items.Sum(x => x.Quantity * x.UnitPrice);
            var discount = items.Sum(x => x.DiscountAmount);
            var tax = items.Sum(x => x.TaxAmount);
            var grandTotal = subtotal - discount + tax;

            var entity = new Invoice
            {
                Id = command.InvoiceId,
                TenantId = command.TenantId,
                PatientId = command.Invoice.PatientId,
                VisitId = command.Invoice.VisitId,
                Subtotal = subtotal,
                DiscountAmount = discount,
                TaxAmount = tax,
                GrandTotal = grandTotal,
                PaidAmount = command.Invoice.PaidAmount,
                RemainingAmount = grandTotal - command.Invoice.PaidAmount,
                Status = command.Invoice.PaidAmount <= 0 ? InvoiceStatus.Draft
                    : command.Invoice.PaidAmount < grandTotal ? InvoiceStatus.PartiallyPaid
                    : InvoiceStatus.Paid,
                InsuranceCompany = command.Invoice.InsuranceCompany,
                InsuranceNumber = command.Invoice.InsuranceNumber,
                ReceiptUrl = existing.ReceiptUrl,
                Notes = command.Invoice.Notes,
                RowVersion = string.IsNullOrWhiteSpace(command.Invoice.RowVersion)
                    ? existing.RowVersion
                    : command.Invoice.RowVersion.FromBase64RowVersion(),
                Items = items
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
                    Message = "Invoice not found.",
                    StatusCode = 404
                };
            }

            return new BaseResponse<object>
            {
                Success = true,
                Message = "Invoice updated.",
                Data = true,
                StatusCode = 200
            };
        }
    }
}
