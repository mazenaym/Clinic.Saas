using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly DapperContext _context;

    public TenantRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<Tenant> AddAsync(Tenant entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        const string sql = @"
INSERT INTO dbo.Tenants
(Id, Name, Subdomain, Email, Phone, LogoUrl, Plan, TimeZone, Currency, IsActive, CreatedAt, UpdatedAt)
VALUES
(@Id, @Name, @Subdomain, @Email, @Phone, @LogoUrl, @Plan, @TimeZone, @Currency, @IsActive, @CreatedAt, @UpdatedAt);

SELECT * FROM dbo.Tenants WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        return await connection.QuerySingleAsync<Tenant>(sql, entity);
    }

    public async Task<Tenant?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT * FROM dbo.Tenants WHERE Id = @Id;";
        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Tenant>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync()
    {
        const string sql = @"SELECT * FROM dbo.Tenants ORDER BY CreatedAt DESC;";
        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Tenant>(sql);
    }

    public async Task UpdateAsync(Tenant entity)
    {
        const string sql = @"
UPDATE dbo.Tenants
SET Name = @Name,
    Subdomain = @Subdomain,
    Email = @Email,
    Phone = @Phone,
    LogoUrl = @LogoUrl,
    Plan = @Plan,
    TimeZone = @TimeZone,
    Currency = @Currency,
    IsActive = @IsActive,
    UpdatedAt = @UpdatedAt
WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"
UPDATE dbo.Tenants
SET IsActive = 0, UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<Tenant?> GetBySubdomainAsync(string subdomain)
    {
        const string sql = @"
SELECT * FROM dbo.Tenants
WHERE LOWER(Subdomain) = LOWER(@Subdomain)
  AND IsActive = 1;";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Tenant>(sql, new { Subdomain = subdomain });
    }
}
