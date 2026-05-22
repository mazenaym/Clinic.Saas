using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Admin.Queries;

public class GetAdminClinicByIdQuery
{
    public class Query
    {
        public Guid ClinicId { get; set; }
    }

    public class Handler
    {
        private readonly IPlatformAdminRepository _repository;

        public Handler(IPlatformAdminRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<AdminClinicDto>> Handle(Query query)
        {
            var clinic = await _repository.GetClinicByIdAsync(query.ClinicId);
            if (clinic is null)
            {
                return new BaseResponse<AdminClinicDto>
                {
                    Success = false,
                    Message = "Clinic was not found",
                    StatusCode = 404
                };
            }

            return new BaseResponse<AdminClinicDto>
            {
                Success = true,
                Message = "Clinic loaded successfully",
                Data = clinic,
                StatusCode = 200
            };
        }
    }
}
