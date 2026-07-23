using AutoMapper;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Onboarding.Queries;
using FluentValidation;

namespace Clinic.Saas.Service.UseCases.Onboarding.Commands;

public class RegisterClinicCommand
{
    public class Command
    {
        public RegisterClinicDto Request { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IPlatformAdminRepository _platformRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordService _passwordService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IMapper _mapper;
        private readonly IValidator<RegisterClinicDto> _validator;

        public Handler(
            IPlatformAdminRepository platformRepository,
            IUserRepository userRepository,
            IPasswordService passwordService,
            IJwtTokenService jwtTokenService,
            IMapper mapper,
            IValidator<RegisterClinicDto> validator)
        {
            _platformRepository = platformRepository;
            _userRepository = userRepository;
            _passwordService = passwordService;
            _jwtTokenService = jwtTokenService;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<BaseResponse<AuthResponseDto>> Handle(Command command)
        {
            var validation = await _validator.ValidateAsync(command.Request);
            if (!validation.IsValid)
            {
                return new BaseResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "Clinic registration data is invalid",
                    Errors = validation.Errors.Select(x => x.ErrorMessage).ToList(),
                    StatusCode = 400
                };
            }

            var subdomain = CheckSubdomainAvailabilityQuery.Handler.NormalizeSubdomain(command.Request.Subdomain);
            if (!CheckSubdomainAvailabilityQuery.Handler.IsFormatValid(subdomain) ||
                CheckSubdomainAvailabilityQuery.Handler.IsReserved(subdomain))
            {
                return Fail("Subdomain is not available", 409);
            }

            if (await _platformRepository.SubdomainExistsAsync(subdomain))
            {
                return Fail("Subdomain is already used", 409);
            }

            var now = DateTime.UtcNow;
            var tenant = new Tenant
            {
                Name = command.Request.ClinicName.Trim(),
                Subdomain = subdomain,
                Email = command.Request.Email.Trim(),
                Phone = command.Request.Phone,
                Plan = command.Request.Plan,
                TimeZone = command.Request.TimeZone.Trim(),
                Currency = command.Request.Currency.Trim().ToUpperInvariant(),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var owner = new User
            {
                FullName = command.Request.OwnerFullName.Trim(),
                Email = command.Request.OwnerEmail.Trim(),
                PasswordHash = _passwordService.HashPassword(command.Request.OwnerPassword),
                Role = UserRole.Admin,
                Phone = command.Request.OwnerPhone,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var subscription = new CreateSubscriptionRequest
            {
                TenantId = tenant.Id,
                Plan = command.Request.Plan,
                StartDate = now,
                EndDate = now.AddDays(14),
                AmountPaid = 0,
                Status = SubscriptionStatus.Trial,
                Notes = "Self-service trial"
            };

            var settings = new ClinicSettingsDto
            {
                OpenTime = command.Request.OpenTime,
                CloseTime = command.Request.CloseTime,
                SlotDurationMin = command.Request.SlotDurationMin,
                ConsultFee = command.Request.ConsultFee,
                TaxPct = command.Request.TaxPct,
                Language = "ar"
            };

            await _platformRepository.CreateClinicAsync(tenant, owner, subscription, settings);

            var accessToken = _jwtTokenService.GenerateAccessToken(owner, tenant);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshExpiry = _jwtTokenService.GetRefreshTokenExpiryUtc();
            await _userRepository.UpdateRefreshTokenAsync(
                owner.TenantId,
                owner.Id,
                _jwtTokenService.HashRefreshToken(refreshToken),
                refreshExpiry);

            return new BaseResponse<AuthResponseDto>
            {
                Success = true,
                Message = "Clinic registered successfully",
                Data = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = _jwtTokenService.GetAccessTokenExpiryUtc(),
                    User = _mapper.Map<UserDto>(owner),
                    Tenant = _mapper.Map<TenantDto>(tenant)
                },
                StatusCode = 201
            };
        }

        private static BaseResponse<AuthResponseDto> Fail(string message, int statusCode) =>
            new()
            {
                Success = false,
                Message = message,
                StatusCode = statusCode
            };
    }
}
