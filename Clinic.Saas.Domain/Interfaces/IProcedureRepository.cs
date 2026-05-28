using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IProcedureRepository
{
    Task<IEnumerable<Procedure>> ListAsync(Guid tenantId, bool includeInactive);
    Task<Procedure?> GetByIdAsync(Guid tenantId, Guid id);
    Task<Procedure> AddAsync(Procedure procedure);
    Task<bool> UpdateAsync(Guid tenantId, Procedure procedure);
    Task<bool> SetActiveAsync(Guid tenantId, Guid id, bool isActive);
    Task<bool> CategoryExistsAsync(Guid tenantId, Guid categoryId);
}
