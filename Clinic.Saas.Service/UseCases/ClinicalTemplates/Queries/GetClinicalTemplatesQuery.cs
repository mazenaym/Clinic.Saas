using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.ClinicalTemplates.Queries;

public class GetClinicalTemplatesQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
    }

    public class Handler
    {
        private readonly IClinicalTemplateRepository _clinicalTemplateRepository;

        public Handler(IClinicalTemplateRepository clinicalTemplateRepository)
        {
            _clinicalTemplateRepository = clinicalTemplateRepository;
        }

        public async Task<BaseResponse<List<ClinicalTemplateDto>>> Handle(Query query)
        {
            var templates = await _clinicalTemplateRepository.GetActiveByTenantAsync(query.TenantId);
            return new BaseResponse<List<ClinicalTemplateDto>>
            {
                Success = true,
                Data = templates.Select(x => new ClinicalTemplateDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Specialty = x.Specialty,
                    ChiefComplaint = x.ChiefComplaint,
                    ClinicalNotes = x.ClinicalNotes,
                    Diagnosis = x.Diagnosis
                }).ToList(),
                Message = "OK",
                StatusCode = 200
            };
        }
    }
}
