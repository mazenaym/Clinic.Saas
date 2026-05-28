using AutoMapper;
using Clinic.Saas.Domain.Exceptions;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.UseCases.Patients.Commands;

public class UpdatePatientCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public UpdatePatientDto Patient { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IPatientRepository _repository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreatePatientDto> _validator;

        public Handler(IPatientRepository repository, IMapper mapper, IValidator<CreatePatientDto> validator)
        {
            _repository = repository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<BaseResponse<PatientDto>> Handle(Command command)
        {
            var existing = await _repository.GetByIdAsync(command.TenantId, command.Patient.Id);
            if (existing is null)
            {
                return new BaseResponse<PatientDto>
                {
                    Success = false,
                    Message = "المريض غير موجود",
                    StatusCode = 404
                };
            }

            var createDto = _mapper.Map<CreatePatientDto>(command.Patient);
            var validationResult = await _validator.ValidateAsync(createDto);
            if (!validationResult.IsValid)
            {
                return new BaseResponse<PatientDto>
                {
                    Success = false,
                    Message = "بيانات غير صحيحة",
                    Errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList(),
                    StatusCode = 400
                };
            }

            var currentRowVersion = existing.RowVersion;
            _mapper.Map(command.Patient, existing);
            existing.RowVersion = string.IsNullOrWhiteSpace(command.Patient.RowVersion)
                ? currentRowVersion
                : command.Patient.RowVersion.FromBase64RowVersion();
            try
            {
                await _repository.UpdateAsync(command.TenantId, existing);
            }
            catch (ConcurrencyConflictException ex)
            {
                return new BaseResponse<PatientDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 409
                };
            }
            catch (RecordNotFoundException ex)
            {
                return new BaseResponse<PatientDto>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                };
            }

            var updated = await _repository.GetByIdAsync(command.TenantId, existing.Id);
            return new BaseResponse<PatientDto>
            {
                Success = true,
                Message = "تم تعديل بيانات المريض بنجاح",
                Data = _mapper.Map<PatientDto>(updated ?? existing),
                StatusCode = 200
            };
        }
    }
}
