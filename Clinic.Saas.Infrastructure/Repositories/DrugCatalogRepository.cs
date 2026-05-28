using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;

public class DrugCatalogRepository : IDrugCatalogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DrugCatalogRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Drug>> SearchAsync(string? term, int take = 20)
    {
        // Drugs is currently a global catalog table: the schema has no TenantId column.
        const string sql = @"
SELECT TOP (@Take) Id, TradeName, GenericName, Category, Strength, Form, Unit, Contraindications, Interactions, IsActive, CreatedAt
FROM dbo.Drugs
WHERE IsActive = 1
  AND (@Term = '' OR TradeName LIKE @Search OR GenericName LIKE @Search)
ORDER BY TradeName;";

        var normalizedTerm = term?.Trim() ?? string.Empty;
        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.QueryAsync<Drug>(sql, new
        {
            Take = take,
            Term = normalizedTerm,
            Search = $"%{normalizedTerm}%"
        });
    }

    public async Task<IEnumerable<Drug>> GetByTradeNamesAsync(IEnumerable<string> tradeNames)
    {
        // Drugs is currently a global catalog table: the schema has no TenantId column.
        var names = tradeNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (names.Length == 0)
        {
            return [];
        }

        const string sql = @"
SELECT Id, TradeName, GenericName, Category, Strength, Form, Unit, Contraindications, Interactions, IsActive, CreatedAt
FROM dbo.Drugs
WHERE IsActive = 1
  AND TradeName IN @Names;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync();
        return await connection.QueryAsync<Drug>(sql, new { Names = names });
    }
}
