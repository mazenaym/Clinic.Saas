using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Payments.Queries;

public class GetPatientPaymentsQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid PatientId { get; set; }
    }

    public class Handler
    {
        private readonly IPaymentRepository _repository;
        private readonly IMapper _mapper;

        public Handler(IPaymentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<BaseResponse<List<PaymentDto>>> Handle(Query query)
        {
            var payments = await _repository.GetByPatientAsync(query.TenantId, query.PatientId);

            return new BaseResponse<List<PaymentDto>>
            {
                Success = true,
                Message = "OK",
                Data = _mapper.Map<List<PaymentDto>>(payments),
                StatusCode = 200
            };
        }
    }
}
