using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Admin.Queries;

public class GetClinicUsageMetricsQuery
{
    public class Handler
    {
        private readonly IAdminReportRepository _repository;

        public Handler(IAdminReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<ClinicUsageMetricDto>>> Handle()
        {
            var rows = await _repository.GetClinicUsageMetricsAsync();
            return new BaseResponse<List<ClinicUsageMetricDto>>
            {
                Success = true,
                Data = rows.ToList(),
                Message = "OK",
                StatusCode = 200
            };
        }
    }
}
