using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[ApiController]
[Route("api/platform/clinics")]
[Authorize(Roles = "SuperAdmin")]
public sealed class PlatformClinicsController(
    IPlatformClinicsFacade facade,
    ISubscriptionService subscriptions,
    ICurrentUserService currentUser) : PlatformControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PlatformClinicFilterDto filter) => OkResponse(await facade.GetAsync(filter));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await facade.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClinicDto dto, [FromQuery] Guid? planId = null)
    {
        var result = await facade.CreateWithInitialSubscriptionAsync(dto, planId, currentUser.UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClinicDto dto)
    {
        var result = await facade.UpdateAsync(id, dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendTenantDto dto) =>
        await subscriptions.SuspendTenantAsync(id, dto.Reason, currentUser.UserId) ? OkResponse(true, "Clinic suspended.") : NotFoundResponse<bool>("Clinic was not found.");

    [HttpPatch("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id) =>
        await subscriptions.ReactivateTenantAsync(id, currentUser.UserId) ? OkResponse(true, "Clinic reactivated.") : StatusCode(409, Success(false, "Clinic needs a valid subscription before reactivation.", 409));

    [HttpPatch("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id) =>
        await subscriptions.DisableTenantAsync(id, "Disabled by platform admin.", currentUser.UserId) ? OkResponse(true, "Clinic disabled.") : NotFoundResponse<bool>("Clinic was not found.");
}
