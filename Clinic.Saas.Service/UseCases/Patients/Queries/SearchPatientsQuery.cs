using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Patients.Queries;

public class SearchPatientsQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
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

        public async Task<BaseResponse<List<PatientDto>>> Handle(Query query)
        {
            var patients = await _repository.SearchAsync(query.TenantId, query.SearchTerm);
            return new BaseResponse<List<PatientDto>>
            {
                Success = true,
                Data = _mapper.Map<List<PatientDto>>(patients),
                StatusCode = 200
            };
        }
    }
}
