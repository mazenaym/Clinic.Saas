using AutoMapper;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.UseCases.Patients.Commands;

public class CreatePatientCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public CreatePatientDto Patient { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IPatientRepository _repository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreatePatientDto> _validator;

        public Handler(
            IPatientRepository repository,
            IMapper mapper,
            IValidator<CreatePatientDto> validator)
        {
            _repository = repository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<BaseResponse<PatientDto>> Handle(Command command)
        {
            // 1. Validate
            var validationResult = await _validator.ValidateAsync(command.Patient);
            if (!validationResult.IsValid)
            {
                return new BaseResponse<PatientDto>
                {
                    Success = false,
                    Message = "بيانات غير صحيحة",
                    Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList(),
                    StatusCode = 400
                };
            }

            // 2. Check if patient exists
            var exists = await _repository.ExistsAsync(
                command.TenantId,
                command.Patient.PhoneNumber
            );

            if (exists)
            {
                return new BaseResponse<PatientDto>
                {
                    Success = false,
                    Message = "المريض موجود بالفعل",
                    StatusCode = 409
                };
            }

            // 3. Map and Add
            var patient = _mapper.Map<Patient>(command.Patient);
            patient.TenantId = command.TenantId;
            patient.PatientCode = await _repository.GenerateNextPatientCodeAsync(command.TenantId);

            var created = await _repository.AddAsync(patient);

            // 4. Map to DTO and Return
            var result = _mapper.Map<PatientDto>(created);

            return new BaseResponse<PatientDto>
            {
                Success = true,
                Message = "تم إضافة المريض بنجاح",
                Data = result,
                StatusCode = 201
            };
        }
    }
}
