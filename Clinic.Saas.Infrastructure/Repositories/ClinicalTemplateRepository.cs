using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;

public class ClinicalTemplateRepository : IClinicalTemplateRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ClinicalTemplateRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<ClinicalTemplate>> GetActiveByTenantAsync(Guid tenantId)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT Id, TenantId, Name, Specialty, ChiefComplaint, ClinicalNotes, Diagnosis, IsActive, CreatedAt, UpdatedAt
FROM dbo.ClinicalTemplates
WHERE TenantId = @TenantId
  AND IsActive = 1
ORDER BY Name;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<ClinicalTemplate>(sql, new { TenantId = tenantId });
    }

    public async Task<Guid> AddAsync(ClinicalTemplate template)
    {
        EnsureTenantId(template.TenantId);

        if (template.Id == Guid.Empty)
        {
            template.Id = Guid.NewGuid();
        }

        const string sql = @"
INSERT INTO dbo.ClinicalTemplates
(
    Id, TenantId, Name, Specialty, ChiefComplaint, ClinicalNotes, Diagnosis,
    IsActive, CreatedAt, UpdatedAt
)
VALUES
(
    @Id, @TenantId, @Name, @Specialty, @ChiefComplaint, @ClinicalNotes, @Diagnosis,
    1, SYSUTCDATETIME(), SYSUTCDATETIME()
);";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        await connection.ExecuteAsync(sql, template);
        return template.Id;
    }

    private static void EnsureTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required.");
        }
    }
}
