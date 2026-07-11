using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[ApiController, Route("api/platform/revenue"), Authorize(Roles="SuperAdmin")]
public sealed class PlatformRevenueController(IPlatformDashboardService dashboard) : PlatformControllerBase
{
    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics([FromQuery] PlatformRevenueAnalyticsFilterDto filter)
    {
        try { return OkResponse(await dashboard.GetRevenueAnalyticsAsync(filter)); }
        catch (ArgumentException ex) { return BadRequest(new BaseResponse<object> { Success=false, Message=ex.Message, StatusCode=400, Errors=[ex.Message] }); }
    }
}
