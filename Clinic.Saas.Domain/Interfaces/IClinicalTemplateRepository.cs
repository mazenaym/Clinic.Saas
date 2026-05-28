using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IClinicalTemplateRepository
{
    Task<IEnumerable<ClinicalTemplate>> GetActiveByTenantAsync(Guid tenantId);
    Task<Guid> AddAsync(ClinicalTemplate template);
}
