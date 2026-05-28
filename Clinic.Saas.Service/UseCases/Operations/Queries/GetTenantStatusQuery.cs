using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Operations.Queries;

public class GetTenantStatusQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
    }

    public class Handler
    {
        private readonly IOperationsTenantRepository _repository;

        public Handler(IOperationsTenantRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<TenantSubscriptionStatusDto>> Handle(Query query)
        {
            var status = await _repository.GetTenantStatusAsync(query.TenantId);
            return new BaseResponse<TenantSubscriptionStatusDto>
            {
                Success = true,
                Data = status,
                Message = "OK",
                StatusCode = 200
            };
        }
    }
}
