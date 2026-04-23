using Clinic.Saas.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Interfaces
{
    public interface IPatientRepository :IBaseRepository<Patient>
    {
        Task<Patient?> GetByPhoneAsync(Guid tenantId, string phone);
        Task<IEnumerable<Patient>> SearchAsync(Guid tenantId, string searchTerm);
        Task<bool> ExistsAsync(Guid tenantId, string phone);
    }
}
