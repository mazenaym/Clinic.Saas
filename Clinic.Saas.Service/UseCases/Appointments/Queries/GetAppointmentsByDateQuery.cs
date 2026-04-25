using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Appointments.Queries;

public class GetAppointmentsByDateQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public DateTime Date { get; set; }
    }

    public class Handler
    {
        private readonly IAppointmentRepository _repository;
        private readonly IMapper _mapper;

        public Handler(IAppointmentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<BaseResponse<List<AppointmentDto>>> Handle(Query query)
        {
            var appointments = await _repository.GetByDateAsync(query.TenantId, query.Date);
            return new BaseResponse<List<AppointmentDto>>
            {
                Success = true,
                Data = _mapper.Map<List<AppointmentDto>>(appointments),
                StatusCode = 200
            };
        }
    }
}
