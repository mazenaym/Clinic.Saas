using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IVisitRepository : IBaseRepository<Visit>
{
    Task<IEnumerable<Visit>> GetByPatientIdAsync(Guid patientId);
}
