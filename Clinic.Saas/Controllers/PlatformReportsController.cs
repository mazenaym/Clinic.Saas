using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[ApiController]
[Route("api/platform/reports")]
[Authorize(Roles = "SuperAdmin")]
public sealed class PlatformReportsController(IPlatformReportsFacade facade) : PlatformControllerBase
{
    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue() => OkResponse(await facade.GetRevenueAsync());
    [HttpGet("subscriptions")]
    public async Task<IActionResult> Subscriptions([FromQuery] PlatformSubscriptionFilterDto filter) => OkResponse(await facade.GetSubscriptionsAsync(filter));
    [HttpGet("clinics-growth")]
    public async Task<IActionResult> ClinicsGrowth([FromQuery] PlatformClinicFilterDto filter) => OkResponse(await facade.GetClinicsAsync(filter));
    [HttpGet("usage")]
    public async Task<IActionResult> Usage([FromQuery] PlatformClinicFilterDto filter) => OkResponse(await facade.GetClinicsAsync(filter));
    [HttpGet("platform")]
    public async Task<IActionResult> Platform([FromQuery] PlatformReportsFilterDto filter) => OkResponse(await facade.GetPlatformAsync(filter));
}
