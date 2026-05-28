using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IDrugCatalogRepository
{
    Task<IEnumerable<Drug>> SearchAsync(string? term, int take = 20);
    Task<IEnumerable<Drug>> GetByTradeNamesAsync(IEnumerable<string> tradeNames);
}
