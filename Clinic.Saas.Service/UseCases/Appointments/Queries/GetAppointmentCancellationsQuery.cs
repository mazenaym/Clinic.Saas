using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Appointments.Queries;

public class GetAppointmentCancellationsQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class Handler
    {
        private readonly IAppointmentRepository _repository;

        public Handler(IAppointmentRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<AppointmentCancellationReportDto>>> Handle(Query query)
        {
            var rows = await _repository.GetCancellationReportAsync(query.TenantId, query.From, query.To.Date.AddDays(1));

            return new BaseResponse<List<AppointmentCancellationReportDto>>
            {
                Success = true,
                Message = "OK",
                Data = rows.Select(x => new AppointmentCancellationReportDto
                {
                    Id = x.Id,
                    AppointmentDate = x.AppointmentDate,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    CancelReason = x.CancelReason,
                    UpdatedAt = x.UpdatedAt
                }).ToList(),
                StatusCode = 200
            };
        }
    }
}
