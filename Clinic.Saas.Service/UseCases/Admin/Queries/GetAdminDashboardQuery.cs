using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Admin.Queries;

public class GetAdminDashboardQuery
{
    public class Handler
    {
        private readonly IPlatformAdminRepository _repository;

        public Handler(IPlatformAdminRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<AdminDashboardStatsDto>> Handle()
        {
            var stats = await _repository.GetDashboardStatsAsync(DateTime.UtcNow);
            return new BaseResponse<AdminDashboardStatsDto>
            {
                Success = true,
                Message = "Admin dashboard loaded successfully",
                Data = stats,
                StatusCode = 200
            };
        }
    }
}
