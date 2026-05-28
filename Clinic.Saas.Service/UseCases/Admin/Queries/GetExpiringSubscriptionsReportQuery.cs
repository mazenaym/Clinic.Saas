using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Admin.Queries;

public class GetExpiringSubscriptionsReportQuery
{
    public class Query
    {
        public int Days { get; set; } = 14;
    }

    public class Handler
    {
        private readonly IAdminReportRepository _repository;

        public Handler(IAdminReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<ExpiringSubscriptionDto>>> Handle(Query query)
        {
            var days = Math.Clamp(query.Days, 1, 365);
            var rows = await _repository.GetExpiringSubscriptionsAsync(days);
            return new BaseResponse<List<ExpiringSubscriptionDto>>
            {
                Success = true,
                Data = rows.ToList(),
                Message = "OK",
                StatusCode = 200
            };
        }
    }
}
