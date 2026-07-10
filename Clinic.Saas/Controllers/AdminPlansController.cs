using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/admin/plans")]
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Obsolete("Compatibility API. Use /api/platform/plans endpoints.")]
public sealed class AdminPlansController(
    IPlatformPlansFacade facade,
    ICurrentUserService currentUser,
    IValidator<CreatePlatformPlanRequest> createValidator,
    IValidator<UpdatePlatformPlanRequest> updateValidator,
    IValidator<UpdatePlatformPlanStatusRequest> statusValidator) : LegacyCompatibilityControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPlans([FromQuery] bool includeInactive = true) => Ok(Wrap(await facade.GetAsync(includeInactive)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPlan(Guid id) => await facade.GetByIdAsync(id) is { } plan ? Ok(Wrap(plan)) : NotFound(Wrap<PlatformPlanDto?>(null, "Plan was not found.", 404));

    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlatformPlanRequest request)
    {
        var invalid = await Validate(createValidator, request);
        if (invalid is not null) return invalid;
        if (await facade.GetByCodeAsync(request.Code) is not null) return Conflict(Failure<PlatformPlanDto>("Plan code already exists.", 409));
        var plan = await facade.CreateAsync(Map(request), currentUser.UserId);
        return StatusCode(201, Wrap(plan, "Plan created.", 201));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlatformPlanRequest request)
    {
        var invalid = await Validate(updateValidator, request);
        if (invalid is not null) return invalid;
        var duplicate = await facade.GetByCodeAsync(request.Code);
        if (duplicate is not null && duplicate.Id != id) return Conflict(Failure<PlatformPlanDto>("Plan code already exists.", 409));
        var plan = await facade.UpdateAsync(id, Map(request), currentUser.UserId);
        return plan is null ? NotFound(Wrap<PlatformPlanDto?>(null, "Plan was not found.", 404)) : Ok(Wrap(plan, "Plan updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePlan(Guid id) => await facade.DeleteAsync(id, currentUser.UserId) switch
    {
        DeletePlanResult.Deleted => Ok(Wrap(true, "Plan deleted.")),
        DeletePlanResult.InUse => Conflict(Failure<bool>("Plan is linked to subscriptions and cannot be deleted.", 409)),
        _ => NotFound(Wrap(false, "Plan was not found.", 404))
    };

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePlatformPlanStatusRequest request)
    {
        var invalid = await Validate(statusValidator, request);
        return invalid ?? await SetStatus(id, request.IsActive, "Plan status updated.");
    }
    [HttpPatch("{id:guid}/activate")]
    public Task<IActionResult> ActivatePlan(Guid id) => SetStatus(id, true, "Plan activated.");
    [HttpPatch("{id:guid}/deactivate")]
    public Task<IActionResult> DeactivatePlan(Guid id) => SetStatus(id, false, "Plan deactivated.");

    private async Task<IActionResult> SetStatus(Guid id, bool active, string message) => await facade.SetActiveAsync(id, active, currentUser.UserId) ? Ok(Wrap(true, message)) : NotFound(Wrap(false, "Plan was not found.", 404));
    private static async Task<IActionResult?> Validate<T>(IValidator<T> validator, T value)
    {
        var result = await validator.ValidateAsync(value);
        return result.IsValid ? null : new ObjectResult(Failure<T>("Validation failed for the request.", 422, result.Errors.Select(x => x.ErrorMessage))) { StatusCode = 422 };
    }
    private static UpsertPlatformPlanDto Map(CreatePlatformPlanRequest x) => new(x.Name, x.Code, x.Description, x.Price, x.Currency, x.DurationDays, x.MaxUsers, x.MaxPatients, x.MaxDoctors, x.FeaturesJson, x.IsActive);
    private static UpsertPlatformPlanDto Map(UpdatePlatformPlanRequest x) => new(x.Name, x.Code, x.Description, x.Price, x.Currency, x.DurationDays, x.MaxUsers, x.MaxPatients, x.MaxDoctors, x.FeaturesJson, x.IsActive);
    private static BaseResponse<T> Wrap<T>(T data, string message = "OK", int code = 200) => new() { Success = code < 400, Message = message, Data = data, StatusCode = code };
    private static BaseResponse<T> Failure<T>(string message, int code, IEnumerable<string>? errors = null) => new() { Success = false, Message = message, StatusCode = code, Errors = errors?.ToList() ?? [] };
}
