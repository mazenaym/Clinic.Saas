using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Payments.Queries;

public class GetDebtTrackingQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
    }

    public class Handler
    {
        private readonly IPaymentRepository _repository;

        public Handler(IPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<PaymentDebtDto>>> Handle(Query query)
        {
            var rows = await _repository.GetDebtTrackingAsync(query.TenantId);

            return new BaseResponse<List<PaymentDebtDto>>
            {
                Success = true,
                Message = "OK",
                Data = rows.Select(x => new PaymentDebtDto
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
