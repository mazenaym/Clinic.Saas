using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[ApiController]
[Route("api/platform/plans")]
[Authorize(Roles = "SuperAdmin")]
public sealed class PlatformPlansController(IPlatformPlansFacade facade, ICurrentUserService currentUser) : PlatformControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool includeInactive = true) => OkResponse(await facade.GetAsync(includeInactive));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => await facade.GetByIdAsync(id) is { } plan ? OkResponse(plan) : NotFoundResponse<PlatformPlanDto>("Plan was not found.");

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertPlatformPlanDto dto)
    {
        var plan = await facade.CreateAsync(dto, currentUser.UserId);
        return StatusCode(201, Success(plan, "Plan created.", 201));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertPlatformPlanDto dto)
    {
        var plan = await facade.UpdateAsync(id, dto, currentUser.UserId);
        if (plan is null) return NotFoundResponse<PlatformPlanDto>("Plan was not found.");
        return OkResponse(plan, "Plan updated.");
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return await facade.DeleteAsync(id, currentUser.UserId) switch
        {
            Clinic.Saas.Domain.Enums.DeletePlanResult.Deleted => OkResponse(true, "Plan deleted."),
            Clinic.Saas.Domain.Enums.DeletePlanResult.InUse => Conflict(new BaseResponse<bool> { Success = false, Message = "Plan is linked to subscriptions and cannot be deleted.", StatusCode = 409 }),
            _ => NotFoundResponse<bool>("Plan was not found.")
        };
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, [FromBody] UpdatePlatformPlanStatusRequest dto)
    {
        if (!await facade.SetActiveAsync(id, dto.IsActive, currentUser.UserId)) return NotFoundResponse<bool>("Plan was not found.");
        return OkResponse(true, "Plan status updated.");
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id) => await facade.SetActiveAsync(id, true, currentUser.UserId) ? OkResponse(true, "Plan activated.") : NotFoundResponse<bool>("Plan was not found.");

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id) => await facade.SetActiveAsync(id, false, currentUser.UserId) ? OkResponse(true, "Plan deactivated.") : NotFoundResponse<bool>("Plan was not found.");
}
