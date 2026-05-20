using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Patients.Queries;

public class GetPatientByIdQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid Id { get; set; }
    }

    public class Handler
    {
        private readonly IPatientRepository _repository;
        private readonly IMapper _mapper;

        public Handler(IPatientRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<BaseResponse<PatientDto>> Handle(Query query)
        {
            var patient = await _repository.GetByIdAsync(query.TenantId, query.Id);

            if (patient is null)
            {
                return new BaseResponse<PatientDto>
                {
                    Success = false,
                    Message = "المريض غير موجود",
                    StatusCode = 404
                };
            }

            return new BaseResponse<PatientDto>
            {
                Success = true,
                Data = _mapper.Map<PatientDto>(patient),
                StatusCode = 200
            };
        }
    }
}
