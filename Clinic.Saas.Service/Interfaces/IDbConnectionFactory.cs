using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Clinic.Saas.Service.Interfaces
{
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> CreateOpenConnectionAsync();
        Task<IDbConnection> CreateOpenTenantConnectionAsync(Guid requestedTenantId);
    }
}
