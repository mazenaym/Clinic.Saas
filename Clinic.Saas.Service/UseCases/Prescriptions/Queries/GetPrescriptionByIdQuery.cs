using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Prescriptions.Queries;

public class GetPrescriptionByIdQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid Id { get; set; }
    }

    public class Handler
    {
        private readonly IPrescriptionRepository _repository;
        private readonly IMapper _mapper;

        public Handler(IPrescriptionRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<BaseResponse<PrescriptionDto>> Handle(Query query)
        {
            var prescription = await _repository.GetByIdAsync(query.TenantId, query.Id);
            if (prescription is null)
            {
                return new BaseResponse<PrescriptionDto>
                {
                    Success = false,
                    Message = "الروشتة غير موجودة",
                    StatusCode = 404
                };
            }

            return new BaseResponse<PrescriptionDto>
            {
                Success = true,
                Data = _mapper.Map<PrescriptionDto>(prescription),
                StatusCode = 200
            };
        }
    }
}
