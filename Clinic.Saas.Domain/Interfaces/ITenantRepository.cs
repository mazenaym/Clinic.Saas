using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface ITenantRepository : IBaseRepository<Tenant>
{
    Task<Tenant?> GetBySubdomainAsync(string subdomain);
}
