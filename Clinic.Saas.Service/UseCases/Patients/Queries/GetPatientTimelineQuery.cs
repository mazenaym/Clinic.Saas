using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Patients.Queries;

public class GetPatientTimelineQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid PatientId { get; set; }
    }

    public class Handler
    {
        private readonly IPatientRepository _repository;

        public Handler(IPatientRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<PatientTimelineItemDto>>> Handle(Query query)
        {
            var patient = await _repository.GetByIdAsync(query.TenantId, query.PatientId);
            if (patient is null)
            {
                return new BaseResponse<List<PatientTimelineItemDto>>
                {
                    Success = false,
                    Message = "Patient not found.",
                    StatusCode = 404
                };
            }

            var rows = await _repository.GetTimelineAsync(query.TenantId, query.PatientId);

            return new BaseResponse<List<PatientTimelineItemDto>>
            {
                Success = true,
                Message = "OK",
                Data = rows.Select(x => new PatientTimelineItemDto
                {
                    Type = x.Type,
                    Id = x.Id,
                    Date = x.Date,
                    Title = x.Title,
                    Details = x.Details
                }).ToList(),
                StatusCode = 200
            };
        }
    }
}
