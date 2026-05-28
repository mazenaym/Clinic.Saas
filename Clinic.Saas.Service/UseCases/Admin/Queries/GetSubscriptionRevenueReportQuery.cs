using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Admin.Queries;

public class GetSubscriptionRevenueReportQuery
{
    public class Handler
    {
        private readonly IAdminReportRepository _repository;

        public Handler(IAdminReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<SubscriptionRevenueDto>>> Handle()
        {
            var rows = await _repository.GetSubscriptionRevenueAsync();
            return new BaseResponse<List<SubscriptionRevenueDto>>
            {
                Success = true,
                Data = rows.ToList(),
                Message = "OK",
                StatusCode = 200
            };
        }
    }
}
