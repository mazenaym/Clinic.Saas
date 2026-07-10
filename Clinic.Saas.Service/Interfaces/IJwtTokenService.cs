using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Service.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, Tenant tenant);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    DateTime GetAccessTokenExpiryUtc();
    DateTime GetRefreshTokenExpiryUtc();
}
