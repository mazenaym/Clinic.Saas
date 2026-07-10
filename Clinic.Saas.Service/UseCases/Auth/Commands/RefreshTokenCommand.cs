using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using FluentValidation;

namespace Clinic.Saas.Service.UseCases.Auth.Commands;

public class RefreshTokenCommand
{
    public class Command
    {
        public RefreshTokenDto Request { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IUserRepository _userRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IMapper _mapper;
        private readonly IValidator<RefreshTokenDto> _validator;

        public Handler(
            IUserRepository userRepository,
            ITenantRepository tenantRepository,
            IJwtTokenService jwtTokenService,
            IMapper mapper,
            IValidator<RefreshTokenDto> validator)
        {
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
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
                    Message = "Refresh token غير صحيح",
                    Errors = validation.Errors.Select(x => x.ErrorMessage).ToList(),
                    StatusCode = 400
                };
            }

            var refreshTokenHash = _jwtTokenService.HashRefreshToken(command.Request.RefreshToken);
            var user = await _userRepository.GetByRefreshTokenAsync(refreshTokenHash);
            if (user is null || string.IsNullOrWhiteSpace(user.RefreshToken))
            {
                return Fail("المستخدم غير موجود", 404);
            }

            if (!string.Equals(user.RefreshToken, refreshTokenHash, StringComparison.Ordinal) ||
                !user.RefreshTokenExpiry.HasValue ||
                user.RefreshTokenExpiry.Value <= DateTime.UtcNow)
            {
                return Fail("Refresh token منتهي أو غير صالح", 401);
            }

            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);
            if (tenant is null)
            {
                return Fail("العيادة غير موجودة", 404);
            }

            var accessToken = _jwtTokenService.GenerateAccessToken(user, tenant);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshExpiry = _jwtTokenService.GetRefreshTokenExpiryUtc();
            await _userRepository.UpdateRefreshTokenAsync(
                user.Id,
                _jwtTokenService.HashRefreshToken(refreshToken),
                refreshExpiry);

            return new BaseResponse<AuthResponseDto>
            {
                Success = true,
                Message = "تم تجديد الجلسة بنجاح",
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
