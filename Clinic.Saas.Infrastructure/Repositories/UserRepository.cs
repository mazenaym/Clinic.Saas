using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Clinic.Saas.Service.Interfaces;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DapperContext _context;
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(DapperContext context, IDbConnectionFactory connectionFactory)
    {
        _context = context;
        _connectionFactory = connectionFactory;
    }

    public async Task<User> AddAsync(User entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        EnsureTenantId(entity.TenantId);
        entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;
        entity.UpdatedAt = entity.UpdatedAt == default ? entity.CreatedAt : entity.UpdatedAt;

        const string sql = @"
INSERT INTO dbo.Users
(
    Id, TenantId, FullName, Email, PasswordHash, Role, Phone, Specialty,
    LicenseNumber, AvatarUrl, RefreshToken, RefreshTokenExpiry, FailedLoginAttempts,
    LockedUntil, IsActive, CreatedAt, UpdatedAt
)
VALUES
(
    @Id, @TenantId, @FullName, @Email, @PasswordHash, @Role, @Phone, @Specialty,
    @LicenseNumber, @AvatarUrl, @RefreshToken, @RefreshTokenExpiry, @FailedLoginAttempts,
    @LockedUntil, @IsActive, @CreatedAt, @UpdatedAt
);

SELECT * FROM dbo.Users WHERE TenantId = @TenantId AND Id = @Id;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QuerySingleAsync<User>(sql, entity);
    }

    public Task<User?> GetByIdAsync(Guid id) =>
        throw new NotSupportedException("Use GetByIdAsync(Guid tenantId, Guid id) for tenant-owned data.");

    public async Task<User?> GetByIdAsync(Guid tenantId, Guid id)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT *
FROM dbo.Users
WHERE TenantId = @TenantId
  AND Id = @Id;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { TenantId = tenantId, Id = id });
    }

    public Task<IEnumerable<User>> GetAllAsync() =>
        throw new NotSupportedException("Use GetByTenantAsync(Guid tenantId) for tenant-owned data.");

    public Task UpdateAsync(User entity) =>
        throw new NotSupportedException("Use explicit tenant-scoped user update methods for tenant-owned data.");

    public Task DeleteAsync(Guid id) =>
        throw new NotSupportedException("Use explicit tenant-scoped user delete/deactivate methods for tenant-owned data.");

    public async Task<User?> GetByEmailAsync(Guid tenantId, string email)
    {
        const string sql = @"
SELECT * FROM dbo.Users
WHERE TenantId = @TenantId
  AND LOWER(Email) = LOWER(@Email);";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { TenantId = tenantId, Email = email });
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        const string sql = @"
SELECT * FROM dbo.Users
WHERE RefreshToken = @RefreshToken;";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { RefreshToken = refreshToken });
    }

    public async Task<IEnumerable<User>> GetByTenantAsync(Guid tenantId)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT * FROM dbo.Users
WHERE TenantId = @TenantId
ORDER BY CreatedAt DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<User>(sql, new { TenantId = tenantId });
    }

    public async Task<bool> ExistsByEmailAsync(Guid tenantId, string email)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT COUNT(1) FROM dbo.Users
WHERE TenantId = @TenantId
  AND LOWER(Email) = LOWER(@Email);";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId, Email = email });
        return count > 0;
    }

    public async Task UpdateRefreshTokenAsync(Guid userId, string? refreshToken, DateTime? expiry)
    {
        const string sql = @"
UPDATE dbo.Users
SET RefreshToken = @RefreshToken,
    RefreshTokenExpiry = @RefreshTokenExpiry,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @UserId;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId, RefreshToken = refreshToken, RefreshTokenExpiry = expiry });
    }

    public async Task IncrementFailedLoginAsync(Guid userId, int failedAttempts, DateTime? lockedUntil)
    {
        const string sql = @"
UPDATE dbo.Users
SET FailedLoginAttempts = @FailedAttempts,
    LockedUntil = @LockedUntil,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @UserId;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId, FailedAttempts = failedAttempts, LockedUntil = lockedUntil });
    }

    public async Task ResetFailedLoginAsync(Guid userId)
    {
        const string sql = @"
UPDATE dbo.Users
SET FailedLoginAttempts = 0,
    LockedUntil = NULL,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @UserId;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId });
    }
    public async Task<User?> GetActiveByIdAsync(Guid tenantId, Guid userId)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT *
FROM dbo.Users
WHERE TenantId = @TenantId
  AND Id = @UserId
  AND IsActive = 1;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new
        {
            TenantId = tenantId,
            UserId = userId
        });
    }

    public async Task<bool> UpdatePasswordAsync(Guid tenantId, Guid userId, string passwordHash)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
UPDATE dbo.Users
SET PasswordHash = @PasswordHash,
    RefreshToken = NULL,
    RefreshTokenExpiry = NULL,
    UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId
  AND Id = @UserId
  AND IsActive = 1;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        var rows = await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            UserId = userId,
            PasswordHash = passwordHash
        });

        return rows > 0;
    }

    private static void EnsureTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required.");
        }
    }
}
