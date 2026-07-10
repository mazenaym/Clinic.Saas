using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using FluentValidation;

namespace Clinic.Saas.Service.UseCases.Auth.Commands;

public class LoginCommand
{
    public class Command
    {
        public LoginDto Request { get; set; } = null!;
    }

    public class Handler
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordService _passwordService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IMapper _mapper;
        private readonly IValidator<LoginDto> _validator;

        public Handler(
            ITenantRepository tenantRepository,
            IUserRepository userRepository,
            IPasswordService passwordService,
            IJwtTokenService jwtTokenService,
            IMapper mapper,
            IValidator<LoginDto> validator)
        {
            _tenantRepository = tenantRepository;
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
                    Message = "بيانات تسجيل الدخول غير صحيحة",
                    Errors = validation.Errors.Select(x => x.ErrorMessage).ToList(),
                    StatusCode = 400
                };
            }

            var tenant = await _tenantRepository.GetBySubdomainAsync(command.Request.Subdomain!);
            if (tenant is null)
            {
                return Fail("العيادة غير موجودة", 404);
            }

            var user = await _userRepository.GetByEmailAsync(tenant.Id, command.Request.Email);
            if (user is null)
            {
                return Fail("بيانات الدخول غير صحيحة", 401);
            }

            if (!user.IsActive)
            {
                return Fail("الحساب غير مفعل", 403);
            }

            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            {
                return Fail("الحساب مقفول مؤقتا بسبب محاولات دخول فاشلة", 423);
            }

            var isValidPassword = _passwordService.VerifyPassword(user.PasswordHash, command.Request.Password);
            if (!isValidPassword)
            {
                var attempts = user.FailedLoginAttempts + 1;
                DateTime? lockout = attempts >= 5 ? DateTime.UtcNow.AddMinutes(15) : null;
                await _userRepository.IncrementFailedLoginAsync(user.TenantId, user.Id, attempts, lockout);
                return Fail("بيانات الدخول غير صحيحة", 401);
            }

            await _userRepository.ResetFailedLoginAsync(user.TenantId, user.Id);

            var accessToken = _jwtTokenService.GenerateAccessToken(user, tenant);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshExpiry = _jwtTokenService.GetRefreshTokenExpiryUtc();
            await _userRepository.UpdateRefreshTokenAsync(
                user.TenantId,
                user.Id,
                _jwtTokenService.HashRefreshToken(refreshToken),
                refreshExpiry);

            return new BaseResponse<AuthResponseDto>
            {
                Success = true,
                Message = "تم تسجيل الدخول بنجاح",
                Data = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = _jwtTokenService.GetAccessTokenExpiryUtc(),
                    User = _mapper.Map<UserDto>(user),
                    Tenant = _mapper.Map<TenantDto>(tenant)
                },
                StatusCode = 200
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
