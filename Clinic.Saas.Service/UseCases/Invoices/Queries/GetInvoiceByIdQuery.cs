using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Invoices;

namespace Clinic.Saas.Service.UseCases.Invoices.Queries;

public class GetInvoiceByIdQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid InvoiceId { get; set; }
    }

    public class Handler
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public Handler(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public async Task<BaseResponse<InvoiceDto>> Handle(Query query)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(query.TenantId, query.InvoiceId);
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
                Message = "OK",
                StatusCode = 200,
                Data = InvoiceMapper.ToDto(invoice)
            };
        }
    }
}
