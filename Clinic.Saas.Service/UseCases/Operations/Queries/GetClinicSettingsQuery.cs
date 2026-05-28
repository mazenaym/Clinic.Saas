using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Operations.Queries;

public class GetClinicSettingsQuery
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

        public async Task<BaseResponse<UpdateClinicSettingsDto>> Handle(Query query)
        {
            var settings = await _repository.GetSettingsAsync(query.TenantId);
            return new BaseResponse<UpdateClinicSettingsDto>
            {
                Success = true,
                Data = settings,
                Message = "OK",
                StatusCode = 200
            };
        }
    }
}
