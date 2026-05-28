using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByIdAsync(Guid tenantId, Guid id);
    Task<User?> GetByEmailAsync(Guid tenantId, string email);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<IEnumerable<User>> GetByTenantAsync(Guid tenantId);
    Task<bool> ExistsByEmailAsync(Guid tenantId, string email);
    Task UpdateRefreshTokenAsync(Guid userId, string? refreshToken, DateTime? expiry);
    Task IncrementFailedLoginAsync(Guid userId, int failedAttempts, DateTime? lockedUntil);
    Task ResetFailedLoginAsync(Guid userId);
    Task<User?> GetActiveByIdAsync(Guid tenantId, Guid userId);
    Task<bool> UpdatePasswordAsync(Guid tenantId, Guid userId, string passwordHash);
    Task<bool> UpdatePreferencesAsync(Guid tenantId, Guid userId, string? avatarUrl);
    Task<bool> IsEmailTakenByAnotherUserAsync(Guid tenantId, Guid userId, string email);
    Task<bool> UpdateAdminUserAsync(Guid tenantId, User user);
    Task<int> CountActiveAdminsAsync(Guid tenantId);
    Task<bool> DeactivateAsync(Guid tenantId, Guid userId);
    Task<bool> ResetPasswordAsync(Guid tenantId, Guid userId, string passwordHash);
}
