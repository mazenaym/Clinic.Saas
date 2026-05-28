using Clinic.Saas.Service.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Clinic.Saas.Infrastructure.Data
{
    public class DapperConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        private readonly ICurrentUserService _currentUser;

        public DapperConnectionFactory(
            IConfiguration configuration,
            ICurrentUserService currentUser)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is missing.");

            _currentUser = currentUser;
        }

        public async Task<IDbConnection> CreateOpenConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task<IDbConnection> CreateOpenTenantConnectionAsync()
        {
            if (!_currentUser.TenantId.HasValue)
            {
                throw new InvalidOperationException("TenantId is required for tenant-scoped database access.");
            }

            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                await connection.ExecuteAsync(@"
EXEC sys.sp_set_session_context @key = N'TenantId', @value = @TenantId, @read_only = 1;
EXEC sys.sp_set_session_context @key = N'UserId', @value = @UserId, @read_only = 1;",
                    new
                    {
                        TenantId = _currentUser.TenantId.Value,
                        UserId = _currentUser.UserId
                    });

                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }
    }
}
