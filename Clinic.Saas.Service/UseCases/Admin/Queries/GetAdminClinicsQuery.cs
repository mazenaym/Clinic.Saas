using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Admin.Queries;

public class GetAdminClinicsQuery
{
    public class Handler
    {
        private readonly IPlatformAdminRepository _repository;

        public Handler(IPlatformAdminRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<IEnumerable<AdminClinicDto>>> Handle()
        {
            var clinics = await _repository.GetClinicsAsync();
            return new BaseResponse<IEnumerable<AdminClinicDto>>
            {
                Success = true,
                Message = "Clinics loaded successfully",
                Data = clinics,
                StatusCode = 200
            };
        }
    }
}
