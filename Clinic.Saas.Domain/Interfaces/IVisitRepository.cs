using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IVisitRepository : IBaseRepository<Visit>
{
    Task<Visit?> GetByIdAsync(Guid tenantId, Guid id);
    Task<IEnumerable<Visit>> GetAllAsync(Guid tenantId);
    Task UpdateAsync(Guid tenantId, Visit entity);
    Task DeleteAsync(Guid tenantId, Guid id, byte[] rowVersion);
    Task<IEnumerable<Visit>> GetByPatientIdAsync(Guid tenantId, Guid patientId);
    Task<int> UpdateClinicalDetailsAsync(Guid tenantId, Guid id, Visit entity);
    Task<int> FinalizeAsync(Guid tenantId, Guid id, Guid finalizedByUserId, byte[] rowVersion);
    Task<int> CountByDateAsync(Guid tenantId, DateTime date);
}
