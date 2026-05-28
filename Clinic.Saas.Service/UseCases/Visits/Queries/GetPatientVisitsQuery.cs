using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Visits.Queries;

public class GetPatientVisitsQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid PatientId { get; set; }
    }

    public class Handler
    {
        private readonly IVisitRepository _repository;
        private readonly IMapper _mapper;

        public Handler(IVisitRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<BaseResponse<List<VisitDto>>> Handle(Query query)
        {
            var visits = await _repository.GetByPatientIdAsync(query.TenantId, query.PatientId);

            return new BaseResponse<List<VisitDto>>
            {
                Success = true,
                Message = "OK",
                Data = _mapper.Map<List<VisitDto>>(visits),
                StatusCode = 200
            };
        }
    }
}
