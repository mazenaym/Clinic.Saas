using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Admin.Queries;

public class GetActivityLogQuery
{
    public class Query
    {
        public int Take { get; set; } = 100;
        public Guid? TenantId { get; set; }
        public bool IncludeAllTenants { get; set; }
    }

    public class Handler
    {
        private readonly IAdminReportRepository _repository;

        public Handler(IAdminReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<AuditLogDto>>> Handle(Query query)
        {
            if (!query.IncludeAllTenants && !query.TenantId.HasValue)
            {
                return new BaseResponse<List<AuditLogDto>>
                {
                    Success = false,
                    Message = "TenantId is required.",
                    StatusCode = 401
                };
            }

            var take = Math.Clamp(query.Take, 1, 500);
            var rows = await _repository.GetActivityLogAsync(
                take,
                query.IncludeAllTenants ? null : query.TenantId);

            return new BaseResponse<List<AuditLogDto>>
            {
                Success = true,
                Data = rows.ToList(),
                Message = "OK",
                StatusCode = 200
            };
        }
    }
}
