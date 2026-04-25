using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IPrescriptionRepository : IBaseRepository<Prescription>
{
    Task<IEnumerable<Prescription>> GetByPatientIdAsync(Guid patientId);
}
