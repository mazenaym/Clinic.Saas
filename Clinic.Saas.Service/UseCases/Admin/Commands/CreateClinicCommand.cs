using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using FluentValidation;

namespace Clinic.Saas.Service.UseCases.Admin.Commands;

public class CreateClinicCommand
{
    public class Command
    {
        public CreateClinicDto Clinic { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IPlatformAdminRepository _repository;
        private readonly IPasswordService _passwordService;
        private readonly IValidator<CreateClinicDto> _validator;

        public Handler(
            IPlatformAdminRepository repository,
            IPasswordService passwordService,
            IValidator<CreateClinicDto> validator)
        {
            _repository = repository;
            _passwordService = passwordService;
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

            var subdomain = command.Clinic.Subdomain.Trim().ToLowerInvariant();
            if (await _repository.SubdomainExistsAsync(subdomain))
            {
                return new BaseResponse<AdminClinicDto>
                {
                    Success = false,
                    Message = "Subdomain is already used",
                    StatusCode = 409
                };
            }

            var now = DateTime.UtcNow;
            var tenant = new Tenant
            {
                Name = command.Clinic.Name.Trim(),
                Subdomain = subdomain,
                Email = command.Clinic.Email.Trim(),
                Phone = command.Clinic.Phone,
                LogoUrl = command.Clinic.LogoUrl,
                Plan = command.Clinic.Plan,
                TimeZone = command.Clinic.TimeZone.Trim(),
                Currency = command.Clinic.Currency.Trim().ToUpperInvariant(),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var owner = new User
            {
                FullName = command.Clinic.OwnerFullName.Trim(),
                Email = command.Clinic.OwnerEmail.Trim(),
                PasswordHash = _passwordService.HashPassword(command.Clinic.OwnerPassword),
                Role = UserRole.Admin,
                Phone = command.Clinic.OwnerPhone,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var subscription = new Subscription
            {
                Plan = command.Clinic.Plan,
                StartDate = command.Clinic.SubscriptionStartDate ?? now,
                EndDate = command.Clinic.SubscriptionEndDate ?? now.AddDays(14),
                AmountPaid = command.Clinic.SubscriptionAmountPaid,
                Status = command.Clinic.SubscriptionStatus,
                PaymentRef = command.Clinic.PaymentRef,
                Notes = command.Clinic.Notes,
                CreatedAt = now
            };

            var settings = new ClinicSettingsDto
            {
                OpenTime = command.Clinic.OpenTime,
                CloseTime = command.Clinic.CloseTime,
                SlotDurationMin = command.Clinic.SlotDurationMin,
                ConsultFee = command.Clinic.ConsultFee,
                TaxPct = command.Clinic.TaxPct
            };

            var created = await _repository.CreateClinicAsync(tenant, owner, subscription, settings);
            return new BaseResponse<AdminClinicDto>
            {
                Success = true,
                Message = "Clinic created successfully",
                Data = created,
                StatusCode = 201
            };
        }
    }
}
