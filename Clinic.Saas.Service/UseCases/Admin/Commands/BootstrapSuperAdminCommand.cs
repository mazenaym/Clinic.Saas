using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using FluentValidation;

namespace Clinic.Saas.Service.UseCases.Admin.Commands;

public class BootstrapSuperAdminCommand
{
    public class Command
    {
        public BootstrapSuperAdminDto Request { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IPlatformAdminRepository _repository;
        private readonly IPasswordService _passwordService;
        private readonly IValidator<BootstrapSuperAdminDto> _validator;

        public Handler(
            IPlatformAdminRepository repository,
            IPasswordService passwordService,
            IValidator<BootstrapSuperAdminDto> validator)
        {
            _repository = repository;
            _passwordService = passwordService;
            _validator = validator;
        }

        public async Task<BaseResponse<AdminClinicDto>> Handle(Command command)
        {
            if (await _repository.SuperAdminExistsAsync())
            {
                return new BaseResponse<AdminClinicDto>
                {
                    Success = false,
                    Message = "Super admin already exists",
                    StatusCode = 409
                };
            }

            var validation = await _validator.ValidateAsync(command.Request);
            if (!validation.IsValid)
            {
                return new BaseResponse<AdminClinicDto>
                {
                    Success = false,
                    Message = "Bootstrap data is invalid",
                    Errors = validation.Errors.Select(x => x.ErrorMessage).ToList(),
                    StatusCode = 400
                };
            }

            var subdomain = command.Request.PlatformSubdomain.Trim().ToLowerInvariant();
            if (await _repository.SubdomainExistsAsync(subdomain))
            {
                return new BaseResponse<AdminClinicDto>
                {
                    Success = false,
                    Message = "Platform subdomain is already used",
                    StatusCode = 409
                };
            }

            var now = DateTime.UtcNow;
            var platformTenant = new Tenant
            {
                Name = command.Request.PlatformName.Trim(),
                Subdomain = subdomain,
                Email = command.Request.PlatformEmail.Trim(),
                Plan = PlanType.Enterprise,
                TimeZone = "Africa/Cairo",
                Currency = "EGP",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var superAdmin = new User
            {
                FullName = command.Request.FullName.Trim(),
                Email = command.Request.Email.Trim(),
                PasswordHash = _passwordService.HashPassword(command.Request.Password),
                Role = UserRole.SuperAdmin,
                Phone = command.Request.Phone,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var created = await _repository.BootstrapSuperAdminAsync(platformTenant, superAdmin);
            return new BaseResponse<AdminClinicDto>
            {
                Success = true,
                Message = "Super admin bootstrapped successfully. Use the platform subdomain to login.",
                Data = created,
                StatusCode = 201
            };
        }
    }
}
