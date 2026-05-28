using Clinic.Saas.Service.Interfaces;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestfactoryController : ControllerBase
    {
        [Authorize]
        [HttpGet("session-context")]
        public async Task<IActionResult> SessionContext([FromServices] IDbConnectionFactory factory)
        {
            using var connection = await factory.CreateOpenTenantConnectionAsync();

            var tenantId = await connection.ExecuteScalarAsync<string>(
                "SELECT CONVERT(nvarchar(50), SESSION_CONTEXT(N'TenantId'));");

            var userId = await connection.ExecuteScalarAsync<string>(
                "SELECT CONVERT(nvarchar(50), SESSION_CONTEXT(N'UserId'));");

            return Ok(new
            {
                tenantId,
                userId
            });
        }
    }
}
