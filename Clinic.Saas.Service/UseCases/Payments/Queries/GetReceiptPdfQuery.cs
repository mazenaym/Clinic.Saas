using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Payments.Queries;

public class GetReceiptPdfQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid PaymentId { get; set; }
    }

    public class Handler
    {
        private readonly IPaymentRepository _repository;
        private readonly IPdfDocumentService _pdf;

        public Handler(IPaymentRepository repository, IPdfDocumentService pdf)
        {
            _repository = repository;
            _pdf = pdf;
        }

        public async Task<BaseResponse<ReceiptPdfDto>> Handle(Query query)
        {
            var payment = await _repository.GetByIdAsync(query.TenantId, query.PaymentId);
            if (payment is null)
            {
                return new BaseResponse<ReceiptPdfDto>
                {
                    Success = false,
                    Message = "Payment not found.",
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
                        ("رقم الفاتورة", payment.InvoiceNumber),
                        ("التاريخ", payment.CreatedAt.ToString("yyyy-MM-dd")),
                        ("الإجمالي", payment.TotalAmount.ToString("N2")),
                        ("المدفوع", payment.PaidAmount.ToString("N2")),
                        ("المتبقي", payment.RemainingAmount.ToString("N2"))]),
                    FileName = $"receipt-{payment.InvoiceNumber}.pdf"
                },
                StatusCode = 200
            };
        }

    }
}
