using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Payments.Queries;

public class GetPaymentByIdQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid PaymentId { get; set; }
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

        public async Task<BaseResponse<PaymentDetailsDto>> Handle(Query query)
        {
            var payment = await _repository.GetByIdAsync(query.TenantId, query.PaymentId);
            if (payment is null)
            {
                return new BaseResponse<PaymentDetailsDto>
                {
                    Success = false,
                    Message = "Payment not found.",
                    StatusCode = 404
                };
            }

            return new BaseResponse<PaymentDetailsDto>
            {
                Success = true,
                Message = "OK",
                Data = _mapper.Map<PaymentDetailsDto>(payment),
                StatusCode = 200
            };
        }
    }
}
