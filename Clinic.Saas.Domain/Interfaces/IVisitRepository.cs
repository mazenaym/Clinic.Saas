using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IVisitRepository : IBaseRepository<Visit>
{
    Task<Visit?> GetByIdAsync(Guid tenantId, Guid id);
    Task<IEnumerable<Visit>> GetAllAsync(Guid tenantId);
    Task<IEnumerable<Visit>> GetByPatientIdAsync(Guid tenantId, Guid patientId);
    Task<int> CountByDateAsync(Guid tenantId, DateTime date);
}
