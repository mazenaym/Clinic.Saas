using AutoMapper;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using FluentValidation;
using System.Text.Json;

namespace Clinic.Saas.Service.UseCases.Visits.Commands;

public class CreateVisitCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public CreateVisitDto Visit { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IVisitRepository _repository;
        private readonly IValidator<CreateVisitDto> _validator;
        private readonly IMapper _mapper;

        public Handler(IVisitRepository repository, IValidator<CreateVisitDto> validator, IMapper mapper)
        {
            _repository = repository;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<BaseResponse<VisitDto>> Handle(Command command)
        {
            var validation = await _validator.ValidateAsync(command.Visit);
            if (!validation.IsValid)
            {
                return new BaseResponse<VisitDto>
                {
                    Success = false,
                    Message = "بيانات الكشف غير صحيحة",
                    Errors = validation.Errors.Select(x => x.ErrorMessage).ToList(),
                    StatusCode = 400
                };
            }

            var entity = new Visit
            {
                TenantId = command.TenantId,
                PatientId = command.Visit.PatientId,
                AppointmentId = command.Visit.AppointmentId,
                DoctorId = command.Visit.DoctorId,
                VisitDate = DateTime.UtcNow,
                VisitType = command.Visit.VisitType,
                ChiefComplaint = command.Visit.ChiefComplaint,
                VitalSigns = command.Visit.VitalSigns is null ? null : JsonSerializer.Serialize(command.Visit.VitalSigns),
                ClinicalNotes = command.Visit.ClinicalNotes,
                Diagnosis = command.Visit.Diagnosis,
                DiagnosisCode = command.Visit.DiagnosisCode,
                FollowUpDate = command.Visit.FollowUpDate,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _repository.AddAsync(entity);
            return new BaseResponse<VisitDto>
            {
                Success = true,
                Message = "تم تسجيل الكشف بنجاح",
                Data = _mapper.Map<VisitDto>(created),
                StatusCode = 201
            };
        }
    }
}
