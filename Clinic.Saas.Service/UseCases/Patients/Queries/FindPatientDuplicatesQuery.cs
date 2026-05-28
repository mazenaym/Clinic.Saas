using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Patients.Queries;

public class FindPatientDuplicatesQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public string? Phone { get; set; }
        public string? NationalId { get; set; }
    }

    public class Handler
    {
        private readonly IPatientRepository _repository;

        public Handler(IPatientRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<PatientDuplicateDto>>> Handle(Query query)
        {
            var rows = await _repository.FindDuplicatesAsync(query.TenantId, query.Phone, query.NationalId);

            return new BaseResponse<List<PatientDuplicateDto>>
            {
                Success = true,
                Message = "OK",
                Data = rows.Select(x => new PatientDuplicateDto
                {
                    Id = x.Id,
                    PatientCode = x.PatientCode,
                    FullName = x.FullName,
                    PhoneNumber = x.PhoneNumber,
                    NationalId = x.NationalId
                }).ToList(),
                StatusCode = 200
            };
        }
    }
}
