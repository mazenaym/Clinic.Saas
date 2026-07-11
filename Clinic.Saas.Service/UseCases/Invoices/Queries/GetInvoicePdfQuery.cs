using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Invoices.Queries;

public static class GetInvoicePdfQuery
{
    public sealed record Query(Guid TenantId, Guid InvoiceId);
    public sealed class Handler(IInvoiceRepository repository, IPdfDocumentService pdf)
    {
        public async Task<BaseResponse<ReceiptPdfDto>> Handle(Query query)
        {
            var invoice = await repository.GetByIdAsync(query.TenantId, query.InvoiceId);
            if (invoice is null) return new() { Success = false, Message = "Invoice not found.", StatusCode = 404 };
            var content = pdf.Generate("فاتورة", [
                ("رقم الفاتورة", invoice.InvoiceNumber), ("المريض", invoice.PatientName),
                ("التاريخ", invoice.CreatedAt.ToString("yyyy-MM-dd")), ("الإجمالي", invoice.GrandTotal.ToString("N2")),
                ("المدفوع", invoice.PaidAmount.ToString("N2")), ("المتبقي", invoice.RemainingAmount.ToString("N2"))],
                invoice.Items.Select(x => $"{x.Description} — {x.Quantity} × {x.UnitPrice:N2} = {x.LineTotal:N2}"));
            return new() { Success = true, Data = new() { Content = content, FileName = $"invoice-{invoice.InvoiceNumber}.pdf" }, Message = "OK", StatusCode = 200 };
        }
    }
}
