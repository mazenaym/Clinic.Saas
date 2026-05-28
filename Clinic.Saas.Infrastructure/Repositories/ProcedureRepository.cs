using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;

public class ProcedureRepository : IProcedureRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProcedureRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Procedure>> ListAsync(Guid tenantId, bool includeInactive)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT
    p.Id,
    p.TenantId,
    p.CategoryId,
    c.Name AS CategoryName,
    p.Name,
    p.Specialty,
    p.DefaultPrice,
    p.IsActive,
    p.CreatedAt,
    p.UpdatedAt
FROM dbo.Procedures p
LEFT JOIN dbo.ProcedureCategories c ON c.TenantId = p.TenantId AND c.Id = p.CategoryId
WHERE p.TenantId = @TenantId
  AND (@IncludeInactive = 1 OR p.IsActive = 1)
ORDER BY p.Name;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<Procedure>(sql, new
        {
            TenantId = tenantId,
            IncludeInactive = includeInactive
        });
    }

    public async Task<Procedure?> GetByIdAsync(Guid tenantId, Guid id)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT
    p.Id,
    p.TenantId,
    p.CategoryId,
    c.Name AS CategoryName,
    p.Name,
    p.Specialty,
    p.DefaultPrice,
    p.IsActive,
    p.CreatedAt,
    p.UpdatedAt
FROM dbo.Procedures p
LEFT JOIN dbo.ProcedureCategories c ON c.TenantId = p.TenantId AND c.Id = p.CategoryId
WHERE p.TenantId = @TenantId
  AND p.Id = @Id;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Procedure>(sql, new { TenantId = tenantId, Id = id });
    }

    public async Task<Procedure> AddAsync(Procedure procedure)
    {
        EnsureTenantId(procedure.TenantId);

        if (procedure.Id == Guid.Empty)
        {
            procedure.Id = Guid.NewGuid();
        }

        const string sql = @"
INSERT INTO dbo.Procedures
(
    Id, TenantId, CategoryId, Name, Specialty, DefaultPrice,
    IsActive, CreatedAt, UpdatedAt
)
VALUES
(
    @Id, @TenantId, @CategoryId, @Name, @Specialty, @DefaultPrice,
    @IsActive, SYSUTCDATETIME(), SYSUTCDATETIME()
);";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        await connection.ExecuteAsync(sql, procedure);
        return await GetByIdAsync(procedure.TenantId, procedure.Id) ?? procedure;
    }

    public async Task<bool> UpdateAsync(Guid tenantId, Procedure procedure)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
UPDATE dbo.Procedures
SET CategoryId = @CategoryId,
    Name = @Name,
    Specialty = @Specialty,
    DefaultPrice = @DefaultPrice,
    UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId
  AND Id = @Id;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        var rows = await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            procedure.Id,
            procedure.CategoryId,
            procedure.Name,
            procedure.Specialty,
            procedure.DefaultPrice
        });

        return rows > 0;
    }

    public async Task<bool> SetActiveAsync(Guid tenantId, Guid id, bool isActive)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
UPDATE dbo.Procedures
SET IsActive = @IsActive,
    UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId
  AND Id = @Id;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        var rows = await connection.ExecuteAsync(sql, new { TenantId = tenantId, Id = id, IsActive = isActive });
        return rows > 0;
    }

    public async Task<bool> CategoryExistsAsync(Guid tenantId, Guid categoryId)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT COUNT(1)
FROM dbo.ProcedureCategories
WHERE TenantId = @TenantId
  AND Id = @CategoryId
  AND IsActive = 1;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId, CategoryId = categoryId });
        return count > 0;
    }

    private static void EnsureTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required.");
        }
    }
}
