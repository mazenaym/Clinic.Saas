using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;

public class PlatformPlanRepository : IPlatformPlanRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PlatformPlanRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<SubscriptionPlan>> GetAllAsync(bool includeInactive = true)
    {
        const string sql = @"
SELECT Id, Name, Code, Description, Price, Currency, DurationDays, MaxUsers, MaxPatients, MaxDoctors,
       FeaturesJson, IsActive, CreatedAtUtc, UpdatedAtUtc
FROM dbo.SubscriptionPlans
WHERE (@IncludeInactive = 1 OR IsActive = 1)
ORDER BY Price, DurationDays, Name;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        var rows = await connection.QueryAsync<SubscriptionPlan>(sql, new { IncludeInactive = includeInactive });
        return rows.ToList();
    }

    public async Task<SubscriptionPlan?> GetByIdAsync(Guid id)
    {
        const string sql = @"
SELECT Id, Name, Code, Description, Price, Currency, DurationDays, MaxUsers, MaxPatients, MaxDoctors,
       FeaturesJson, IsActive, CreatedAtUtc, UpdatedAtUtc
FROM dbo.SubscriptionPlans
WHERE Id = @Id;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<SubscriptionPlan>(sql, new { Id = id });
    }

    public async Task<SubscriptionPlan?> GetByCodeAsync(string code)
    {
        const string sql = @"
SELECT Id, Name, Code, Description, Price, Currency, DurationDays, MaxUsers, MaxPatients, MaxDoctors,
       FeaturesJson, IsActive, CreatedAtUtc, UpdatedAtUtc
FROM dbo.SubscriptionPlans
WHERE Code = @Code;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<SubscriptionPlan>(sql, new { Code = code });
    }

    public async Task<SubscriptionPlan> CreateAsync(SubscriptionPlan plan)
    {
        const string sql = @"
INSERT INTO dbo.SubscriptionPlans
(Id, Name, Code, Description, Price, Currency, DurationDays, MaxUsers, MaxPatients, MaxDoctors, FeaturesJson, IsActive, CreatedAtUtc)
VALUES
(@Id, @Name, @Code, @Description, @Price, @Currency, @DurationDays, @MaxUsers, @MaxPatients, @MaxDoctors, @FeaturesJson, @IsActive, SYSUTCDATETIME());

SELECT Id, Name, Code, Description, Price, Currency, DurationDays, MaxUsers, MaxPatients, MaxDoctors,
       FeaturesJson, IsActive, CreatedAtUtc, UpdatedAtUtc
FROM dbo.SubscriptionPlans WHERE Id = @Id;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.QuerySingleAsync<SubscriptionPlan>(sql, plan);
    }

    public async Task<SubscriptionPlan?> UpdateAsync(SubscriptionPlan plan)
    {
        const string sql = @"
UPDATE dbo.SubscriptionPlans
SET Name = @Name,
    Code = @Code,
    Description = @Description,
    Price = @Price,
    Currency = @Currency,
    DurationDays = @DurationDays,
    MaxUsers = @MaxUsers,
    MaxPatients = @MaxPatients,
    MaxDoctors = @MaxDoctors,
    FeaturesJson = @FeaturesJson,
    IsActive = @IsActive,
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE Id = @Id;

SELECT Id, Name, Code, Description, Price, Currency, DurationDays, MaxUsers, MaxPatients, MaxDoctors,
       FeaturesJson, IsActive, CreatedAtUtc, UpdatedAtUtc
FROM dbo.SubscriptionPlans WHERE Id = @Id;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<SubscriptionPlan>(sql, plan);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM dbo.SubscriptionPlans WHERE Id = @Id;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.ExecuteAsync(sql, new { Id = id }) > 0;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, bool isActive)
    {
        const string sql = @"
UPDATE dbo.SubscriptionPlans
SET IsActive = @IsActive, UpdatedAtUtc = SYSUTCDATETIME()
WHERE Id = @Id;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.ExecuteAsync(sql, new { Id = id, IsActive = isActive }) > 0;
    }
}
