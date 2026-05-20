using Clinic.Saas.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Interfaces
{
    public interface IPatientRepository :IBaseRepository<Patient>
    {
        Task<Patient?> GetByIdAsync(Guid tenantId, Guid id);
        Task<IEnumerable<Patient>> GetAllAsync(Guid tenantId);
        Task UpdateAsync(Guid tenantId, Patient entity);
        Task DeleteAsync(Guid tenantId, Guid id);
        Task<Patient?> GetByPhoneAsync(Guid tenantId, string phone);
        Task<IEnumerable<Patient>> SearchAsync(Guid tenantId, string searchTerm);
        Task<bool> ExistsAsync(Guid tenantId, string phone);
        Task<string> GenerateNextPatientCodeAsync(Guid tenantId);
    }
}
