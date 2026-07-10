using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/admin/plans")]
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Obsolete("Compatibility API. Use /api/platform/plans endpoints.")]
public sealed class AdminPlansController(IPlatformPlansFacade facade) : LegacyCompatibilityControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPlans([FromQuery] bool includeInactive = true) => Ok(Wrap(await facade.GetAsync(includeInactive)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPlan(Guid id) => await facade.GetAsync(id) is { } plan ? Ok(Wrap(plan)) : NotFound(Wrap<PlatformPlanDto?>(null, "Plan was not found.", 404));

    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlatformPlanRequest request)
    {
        var result = await facade.CreateLegacyAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlatformPlanRequest request)
    {
        var result = await facade.UpdateLegacyAsync(id, request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        var result = await facade.DeleteLegacyAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:guid}/status")]
    public Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePlatformPlanStatusRequest request) => SetStatus(id, request, "Plan status updated.");
    [HttpPatch("{id:guid}/activate")]
    public Task<IActionResult> ActivatePlan(Guid id) => SetStatus(id, new UpdatePlatformPlanStatusRequest(true), "Plan activated.");
    [HttpPatch("{id:guid}/deactivate")]
    public Task<IActionResult> DeactivatePlan(Guid id) => SetStatus(id, new UpdatePlatformPlanStatusRequest(false), "Plan deactivated.");

    private async Task<IActionResult> SetStatus(Guid id, UpdatePlatformPlanStatusRequest request, string message)
    {
        var result = await facade.SetLegacyStatusAsync(id, request, message);
        return StatusCode(result.StatusCode, result);
    }
    private static BaseResponse<T> Wrap<T>(T data, string message = "OK", int code = 200) => new() { Success = code < 400, Message = message, Data = data, StatusCode = code };
}
