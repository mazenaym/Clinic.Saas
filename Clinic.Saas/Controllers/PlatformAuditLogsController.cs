using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[ApiController]
[Route("api/platform/audit-logs")]
[Authorize(Roles = "SuperAdmin")]
public sealed class PlatformAuditLogsController(IPlatformAuditLogsFacade facade) : PlatformControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int pageSize = 100)
    {
        var result = await facade.GetAsync(pageSize, true, null);
        return StatusCode(result.StatusCode, result);
    }
}
