using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DapperContext _context;

    public UserRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<User> AddAsync(User entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

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

SELECT * FROM dbo.Users WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        return await connection.QuerySingleAsync<User>(sql, entity);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT * FROM dbo.Users WHERE Id = @Id;";
        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        const string sql = @"SELECT * FROM dbo.Users ORDER BY CreatedAt DESC;";
        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<User>(sql);
    }

    public async Task UpdateAsync(User entity)
    {
        const string sql = @"
UPDATE dbo.Users
SET FullName = @FullName,
    Email = @Email,
    PasswordHash = @PasswordHash,
    Role = @Role,
    Phone = @Phone,
    Specialty = @Specialty,
    LicenseNumber = @LicenseNumber,
    AvatarUrl = @AvatarUrl,
    RefreshToken = @RefreshToken,
    RefreshTokenExpiry = @RefreshTokenExpiry,
    FailedLoginAttempts = @FailedLoginAttempts,
    LockedUntil = @LockedUntil,
    IsActive = @IsActive,
    UpdatedAt = @UpdatedAt
WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"
UPDATE dbo.Users
SET IsActive = 0, UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

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
        const string sql = @"
SELECT * FROM dbo.Users
WHERE TenantId = @TenantId
ORDER BY CreatedAt DESC;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<User>(sql, new { TenantId = tenantId });
    }

    public async Task<bool> ExistsByEmailAsync(Guid tenantId, string email)
    {
        const string sql = @"
SELECT COUNT(1) FROM dbo.Users
WHERE TenantId = @TenantId
  AND LOWER(Email) = LOWER(@Email);";

        using var connection = _context.CreateConnection();
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
        const string sql = @"
SELECT *
FROM dbo.Users
WHERE TenantId = @TenantId
  AND Id = @UserId
  AND IsActive = 1;";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new
        {
            TenantId = tenantId,
            UserId = userId
        });
    }

    public async Task<bool> UpdatePasswordAsync(Guid tenantId, Guid userId, string passwordHash)
    {
        const string sql = @"
UPDATE dbo.Users
SET PasswordHash = @PasswordHash,
    RefreshToken = NULL,
    RefreshTokenExpiry = NULL,
    UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId
  AND Id = @UserId
  AND IsActive = 1;";

        using var connection = _context.CreateConnection();
        var rows = await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            UserId = userId,
            PasswordHash = passwordHash
        });

        return rows > 0;
    }
}
