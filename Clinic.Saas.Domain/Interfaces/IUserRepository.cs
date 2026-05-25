using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(Guid tenantId, string email);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<IEnumerable<User>> GetByTenantAsync(Guid tenantId);
    Task<bool> ExistsByEmailAsync(Guid tenantId, string email);
    Task UpdateRefreshTokenAsync(Guid userId, string? refreshToken, DateTime? expiry);
    Task IncrementFailedLoginAsync(Guid userId, int failedAttempts, DateTime? lockedUntil);
    Task ResetFailedLoginAsync(Guid userId);
    Task<User?> GetActiveByIdAsync(Guid tenantId, Guid userId);
    Task<bool> UpdatePasswordAsync(Guid tenantId, Guid userId, string passwordHash);
}
