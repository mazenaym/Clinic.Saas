using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using FluentValidation;

namespace Clinic.Saas.Service.UseCases.Admin.Commands;

public class UpdateClinicCommand
{
    public class Command
    {
        public Guid ClinicId { get; set; }
        public UpdateClinicDto Clinic { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IPlatformAdminRepository _repository;
        private readonly IValidator<UpdateClinicDto> _validator;

        public Handler(IPlatformAdminRepository repository, IValidator<UpdateClinicDto> validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<BaseResponse<AdminClinicDto>> Handle(Command command)
        {
            var validation = await _validator.ValidateAsync(command.Clinic);
            if (!validation.IsValid)
            {
                return new BaseResponse<AdminClinicDto>
                {
                    Success = false,
                    Message = "Clinic data is invalid",
                    Errors = validation.Errors.Select(x => x.ErrorMessage).ToList(),
                    StatusCode = 400
                };
            }

            var current = await _repository.GetClinicByIdAsync(command.ClinicId);
            if (current is null)
            {
                return new BaseResponse<AdminClinicDto>
                {
                    Success = false,
                    Message = "Clinic was not found",
                    StatusCode = 404
                };
            }

            var subdomain = command.Clinic.Subdomain.Trim().ToLowerInvariant();
            if (await _repository.SubdomainExistsAsync(subdomain, command.ClinicId))
            {
                return new BaseResponse<AdminClinicDto>
                {
                    Success = false,
                    Message = "Subdomain is already used",
                    StatusCode = 409
                };
            }

            await _repository.UpdateClinicAsync(new Tenant
            {
                Id = command.ClinicId,
                Name = command.Clinic.Name.Trim(),
                Subdomain = subdomain,
                Email = command.Clinic.Email.Trim(),
                Phone = command.Clinic.Phone,
                LogoUrl = command.Clinic.LogoUrl,
                Plan = command.Clinic.Plan,
                TimeZone = command.Clinic.TimeZone.Trim(),
                Currency = command.Clinic.Currency.Trim().ToUpperInvariant(),
                IsActive = command.Clinic.IsActive,
                UpdatedAt = DateTime.UtcNow
            });

            var updated = await _repository.GetClinicByIdAsync(command.ClinicId);
            return new BaseResponse<AdminClinicDto>
            {
                Success = true,
                Message = "Clinic updated successfully",
                Data = updated,
                StatusCode = 200
            };
        }
    }
}
