using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Appointments.Queries;

public class GetAppointmentRangeQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Guid? DoctorId { get; set; }
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
            var appointments = await _repository.GetByDateRangeAsync(query.TenantId, query.From, query.To);
            if (query.DoctorId.HasValue)
            {
                appointments = appointments.Where(x => x.DoctorId == query.DoctorId.Value);
            }

            return new BaseResponse<List<AppointmentDto>>
            {
                Success = true,
                Message = "OK",
                Data = _mapper.Map<List<AppointmentDto>>(appointments),
                StatusCode = 200
            };
        }
    }
}
