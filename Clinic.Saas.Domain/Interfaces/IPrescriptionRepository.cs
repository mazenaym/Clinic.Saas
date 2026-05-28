using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IPrescriptionRepository : IBaseRepository<Prescription>
{
    Task<Prescription?> GetByIdAsync(Guid tenantId, Guid id);
    Task UpdateAsync(Guid tenantId, Prescription entity);
    Task DeleteAsync(Guid tenantId, Guid id);
    Task<IEnumerable<Prescription>> GetByPatientIdAsync(Guid tenantId, Guid patientId);
}
