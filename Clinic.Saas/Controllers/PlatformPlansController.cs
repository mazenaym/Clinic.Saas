using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Clinic.Saas.api.Controllers;

[ApiController]
[Route("api/platform/plans")]
[Authorize(Roles = "SuperAdmin")]
public sealed class PlatformPlansController(IPlatformPlansFacade facade, ICurrentUserService currentUser, IAuditService audit) : PlatformControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool includeInactive = true) => OkResponse(await facade.GetAsync(includeInactive));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => await facade.GetAsync(id) is { } plan ? OkResponse(plan) : NotFoundResponse<PlatformPlanDto>("Plan was not found.");

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertPlatformPlanDto dto)
    {
        var plan = await facade.CreateAsync(dto);
        await LogAsync("CreatePlan", plan.Id, plan);
        return StatusCode(201, Success(plan, "Plan created.", 201));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertPlatformPlanDto dto)
    {
        var plan = await facade.UpdateAsync(id, dto);
        if (plan is null) return NotFoundResponse<PlatformPlanDto>("Plan was not found.");
        await LogAsync("UpdatePlan", id, plan);
        return OkResponse(plan, "Plan updated.");
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await facade.DeleteAsync(id)) return NotFoundResponse<bool>("Plan was not found.");
        await LogAsync("DeletePlan", id, new { id });
        return OkResponse(true, "Plan deleted.");
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, [FromBody] UpdatePlatformPlanStatusRequest dto)
    {
        if (!await facade.SetActiveAsync(id, dto.IsActive)) return NotFoundResponse<bool>("Plan was not found.");
        await LogAsync(dto.IsActive ? "ActivatePlan" : "DeactivatePlan", id, new { id, dto.IsActive });
        return OkResponse(true, "Plan status updated.");
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id) => await facade.SetActiveAsync(id, true) ? OkResponse(true, "Plan activated.") : NotFoundResponse<bool>("Plan was not found.");

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id) => await facade.SetActiveAsync(id, false) ? OkResponse(true, "Plan deactivated.") : NotFoundResponse<bool>("Plan was not found.");

    private Task LogAsync(string action, Guid id, object values) => audit.LogAsync(new AuditEntry { UserId = currentUser.UserId, Action = action, EntityName = "SubscriptionPlan", EntityId = id, NewValues = JsonSerializer.Serialize(values), CreatedAt = DateTime.UtcNow });
}
