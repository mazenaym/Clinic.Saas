using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[ApiController]
[Route("api/platform/dashboard")]
[Authorize(Roles = "SuperAdmin")]
public sealed class PlatformDashboardController(IPlatformDashboardFacade facade) : PlatformControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary() => OkResponse(await facade.GetSummaryAsync());
}
