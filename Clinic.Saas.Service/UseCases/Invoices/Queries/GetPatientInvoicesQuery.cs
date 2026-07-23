using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Invoices;

namespace Clinic.Saas.Service.UseCases.Invoices.Queries;

public class GetPatientInvoicesQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid PatientId { get; set; }
    }

    public class Handler
    {
        private readonly IInvoiceRepository _repository;

        public Handler(IInvoiceRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<InvoiceDto>>> Handle(Query query)
        {
            var invoices = await _repository.GetByPatientAsync(query.TenantId, query.PatientId);

            return new BaseResponse<List<InvoiceDto>>
            {
                Success = true,
                Message = "OK",
                Data = invoices.Select(InvoiceMapper.ToDto).ToList(),
                StatusCode = 200
            };
        }
    }
}
