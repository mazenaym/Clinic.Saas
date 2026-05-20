using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Visits.Queries;

public class GetVisitByIdQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid Id { get; set; }
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

        public async Task<BaseResponse<VisitDto>> Handle(Query query)
        {
            var visit = await _repository.GetByIdAsync(query.TenantId, query.Id);
            if (visit is null)
            {
                return new BaseResponse<VisitDto>
                {
                    Success = false,
                    Message = "الكشف غير موجود",
                    StatusCode = 404
                };
            }

            return new BaseResponse<VisitDto>
            {
                Success = true,
                Data = _mapper.Map<VisitDto>(visit),
                StatusCode = 200
            };
        }
    }
}
