using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Payments.Queries;

public class GetMonthlyRevenueQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
    }

    public class Handler
    {
        private readonly IPaymentRepository _repository;

        public Handler(IPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<MonthlyRevenueDto>>> Handle(Query query)
        {
            var start = new DateTime(query.Year, query.Month, 1);
            var end = start.AddMonths(1);
            var rows = await _repository.GetMonthlyRevenueAsync(query.TenantId, start, end);

            return new BaseResponse<List<MonthlyRevenueDto>>
            {
                Success = true,
                Message = "OK",
                Data = rows.Select(x => new MonthlyRevenueDto
                {
                    Date = x.Date,
                    PaidAmount = x.PaidAmount,
                    RemainingAmount = x.RemainingAmount,
                    InvoiceCount = x.InvoiceCount
                }).ToList(),
                StatusCode = 200
            };
        }
    }
}
