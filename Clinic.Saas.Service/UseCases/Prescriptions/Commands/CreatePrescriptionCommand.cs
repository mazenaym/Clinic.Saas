using AutoMapper;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.UseCases.Prescriptions.Commands;

public class CreatePrescriptionCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public CreatePrescriptionDto Prescription { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IPrescriptionRepository _repository;
        private readonly IValidator<CreatePrescriptionDto> _validator;
        private readonly IMapper _mapper;

        public Handler(IPrescriptionRepository repository, IValidator<CreatePrescriptionDto> validator, IMapper mapper)
        {
            _repository = repository;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<BaseResponse<PrescriptionDto>> Handle(Command command)
        {
            var validation = await _validator.ValidateAsync(command.Prescription);
            if (!validation.IsValid)
            {
                return new BaseResponse<PrescriptionDto>
                {
                    Success = false,
                    Message = "بيانات الروشتة غير صحيحة",
                    Errors = validation.Errors.Select(x => x.ErrorMessage).ToList(),
                    StatusCode = 400
                };
            }

            var entity = new Prescription
            {
                TenantId = command.TenantId,
                VisitId = command.Prescription.VisitId,
                PatientId = command.Prescription.PatientId,
                DoctorId = command.Prescription.DoctorId,
                Notes = command.Prescription.Notes,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Items = command.Prescription.Items.Select((item, index) => new PrescriptionItem
                {
                    DrugId = item.DrugId,
                    DrugName = item.DrugName,
                    Dosage = item.Dosage,
                    Frequency = item.Frequency,
                    Duration = item.Duration,
                    Route = item.Route,
                    Instructions = item.Instructions,
                    SortOrder = index + 1
                }).ToList()
            };

            var created = await _repository.AddAsync(entity);
            var dto = _mapper.Map<PrescriptionDto>(created);
            dto.PdfUrl = $"/api/prescriptions/{created.Id}/pdf";
            return new BaseResponse<PrescriptionDto>
            {
                Success = true,
                Message = "تم إنشاء الروشتة بنجاح",
                Data = dto,
                StatusCode = 201
            };
        }
    }
}
