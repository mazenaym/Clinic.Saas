using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Invoices.Queries;

public class GetReceiptPdfQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid InvoiceId { get; set; }
    }

    public class Handler
    {
        private readonly IInvoiceRepository _repository;
        private readonly IPdfDocumentService _pdf;

        public Handler(IInvoiceRepository repository, IPdfDocumentService pdf)
        {
            _repository = repository;
            _pdf = pdf;
        }

        public async Task<BaseResponse<ReceiptPdfDto>> Handle(Query query)
        {
            var invoice = await _repository.GetByIdAsync(query.TenantId, query.InvoiceId);
            if (invoice is null)
            {
                return new BaseResponse<ReceiptPdfDto>
                {
                    Success = false,
                    Message = "Invoice not found.",
                    StatusCode = 404
                };
            }

            return new BaseResponse<ReceiptPdfDto>
            {
                Success = true,
                Message = "OK",
                Data = new ReceiptPdfDto
                {
                    Content = _pdf.Generate("إيصال دفع", [
                        ("رقم الفاتورة", invoice.InvoiceNumber),
                        ("التاريخ", invoice.CreatedAt.ToString("yyyy-MM-dd")),
                        ("الإجمالي", invoice.GrandTotal.ToString("N2")),
                        ("المدفوع", invoice.PaidAmount.ToString("N2")),
                        ("المتبقي", invoice.RemainingAmount.ToString("N2"))]),
                    FileName = $"receipt-{invoice.InvoiceNumber}.pdf"
                },
                StatusCode = 200
            };
        }
    }
}
