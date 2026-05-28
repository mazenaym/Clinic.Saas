using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.ClinicalTemplates.Commands;

public class CreateClinicalTemplateCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public CreateClinicalTemplateDto Template { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IClinicalTemplateRepository _clinicalTemplateRepository;

        public Handler(IClinicalTemplateRepository clinicalTemplateRepository)
        {
            _clinicalTemplateRepository = clinicalTemplateRepository;
        }

        public async Task<BaseResponse<object>> Handle(Command command)
        {
            var id = await _clinicalTemplateRepository.AddAsync(new ClinicalTemplate
            {
                TenantId = command.TenantId,
                Name = command.Template.Name,
                Specialty = command.Template.Specialty,
                ChiefComplaint = command.Template.ChiefComplaint,
                ClinicalNotes = command.Template.ClinicalNotes,
                Diagnosis = command.Template.Diagnosis,
                IsActive = true
            });

            return new BaseResponse<object>
            {
                Success = true,
                Data = new { id },
                Message = "OK",
                StatusCode = 200
            };
        }
    }
}
