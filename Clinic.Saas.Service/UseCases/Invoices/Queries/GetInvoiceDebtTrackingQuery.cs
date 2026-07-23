using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Invoices.Queries;

public class GetInvoiceDebtTrackingQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
    }

    public class Handler
    {
        private readonly IInvoiceRepository _repository;

        public Handler(IInvoiceRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<InvoiceDebtDto>>> Handle(Query query)
        {
            var rows = await _repository.GetDebtTrackingAsync(query.TenantId);

            return new BaseResponse<List<InvoiceDebtDto>>
            {
                Success = true,
                Message = "OK",
                Data = rows.Select(x => new InvoiceDebtDto
                {
                    PatientId = x.PatientId,
                    FullName = x.FullName,
                    PhoneNumber = x.PhoneNumber,
                    TotalDebt = x.TotalDebt
                }).ToList(),
                StatusCode = 200
            };
        }
    }
}
