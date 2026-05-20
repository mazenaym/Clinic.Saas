using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IPrescriptionRepository : IBaseRepository<Prescription>
{
    Task<Prescription?> GetByIdAsync(Guid tenantId, Guid id);
    Task<IEnumerable<Prescription>> GetByPatientIdAsync(Guid tenantId, Guid patientId);
}
