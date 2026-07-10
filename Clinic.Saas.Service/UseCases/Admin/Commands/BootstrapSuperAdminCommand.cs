using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

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
        private readonly IAuditService _audit;
        private readonly Microsoft.Extensions.Logging.ILogger<Handler> _logger;

        public Handler(
            IPlatformAdminRepository repository,
            IPasswordService passwordService,
            IValidator<BootstrapSuperAdminDto> validator, IAuditService audit, Microsoft.Extensions.Logging.ILogger<Handler> logger)
        {
            _repository = repository;
            _passwordService = passwordService;
            _validator = validator;
            _audit = audit;
            _logger = logger;
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
            if (created is null)
            {
                return new BaseResponse<AdminClinicDto> { Success = false, Message = "Super admin already exists", StatusCode = 409 };
            }
            try { await _audit.LogAsync(new AuditEntry { TenantId = created.Id, UserId = superAdmin.Id, Action = "BootstrapSuperAdmin", EntityName = "Tenant", EntityId = created.Id, CreatedAt = DateTime.UtcNow }); }
            catch (Exception ex) { _logger.LogError(ex, "Audit failed after SuperAdmin bootstrap for tenant {TenantId}", created.Id); }
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
