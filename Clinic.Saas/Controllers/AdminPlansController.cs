using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.Common;

namespace Clinic.Saas.api.Controllers;

[Route("api/admin/plans")]
[ApiController]
[Authorize(Roles = "SuperAdmin")]
public class AdminPlansController : ControllerBase
{
    private readonly IPlanService _plans;
    private readonly IValidator<CreatePlatformPlanRequest> _createValidator;
    private readonly IValidator<UpdatePlatformPlanRequest> _updateValidator;
    private readonly IValidator<UpdatePlatformPlanStatusRequest> _statusValidator;

    public AdminPlansController(
        IPlanService plans,
        IValidator<CreatePlatformPlanRequest> createValidator,
        IValidator<UpdatePlatformPlanRequest> updateValidator,
        IValidator<UpdatePlatformPlanStatusRequest> statusValidator)
    {
        _plans = plans;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlans([FromQuery] bool includeInactive = true)
    {
        return Ok(Success(await _plans.GetPlansAsync(includeInactive)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPlan(Guid id)
    {
        var plan = await _plans.GetPlanByIdAsync(id);
        return plan is null ? NotFound(Success<PlatformPlanDto?>(null, "Plan was not found.", 404)) : Ok(Success(plan));
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlatformPlanRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return StatusCode(422, Failure<PlatformPlanDto>("Validation failed for the request.", 422, validation.Errors.Select(x => x.ErrorMessage)));
        }

        var dto = ToUpsert(request);
        var duplicate = await _plans.GetPlanByCodeAsync(dto.Code);
        if (duplicate is not null)
        {
            return Conflict(Failure<PlatformPlanDto>("Plan code already exists.", 409));
        }

        var plan = await _plans.CreatePlanAsync(dto);
        return StatusCode(201, Success(plan, "Plan created.", 201));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlatformPlanRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return StatusCode(422, Failure<PlatformPlanDto>("Validation failed for the request.", 422, validation.Errors.Select(x => x.ErrorMessage)));
        }

        var dto = ToUpsert(request);
        var duplicate = await _plans.GetPlanByCodeAsync(dto.Code);
        if (duplicate is not null && duplicate.Id != id)
        {
            return Conflict(Failure<PlatformPlanDto>("Plan code already exists.", 409));
        }

        var plan = await _plans.UpdatePlanAsync(id, dto);
        return plan is null ? NotFound(Success<PlatformPlanDto?>(null, "Plan was not found.", 404)) : Ok(Success(plan, "Plan updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        try
        {
            return await _plans.DeletePlanAsync(id)
                ? Ok(Success(true, "Plan deleted."))
                : NotFound(Success(false, "Plan was not found.", 404));
        }
        catch (DbException)
        {
            return Conflict(Failure<bool>("Plan is linked to subscriptions and cannot be deleted.", 409));
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePlatformPlanStatusRequest request)
    {
        var validation = await _statusValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return StatusCode(422, Failure<bool>("Validation failed for the request.", 422, validation.Errors.Select(x => x.ErrorMessage)));
        }

        return await _plans.SetPlanActiveAsync(id, request.IsActive)
            ? Ok(Success(true, "Plan status updated."))
            : NotFound(Success(false, "Plan was not found.", 404));
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> ActivatePlan(Guid id)
    {
        return await _plans.SetPlanActiveAsync(id, true)
            ? Ok(Success(true, "Plan activated."))
            : NotFound(Success(false, "Plan was not found.", 404));
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivatePlan(Guid id)
    {
        return await _plans.SetPlanActiveAsync(id, false)
            ? Ok(Success(true, "Plan deactivated."))
            : NotFound(Success(false, "Plan was not found.", 404));
    }

    private static UpsertPlatformPlanDto ToUpsert(CreatePlatformPlanRequest request) => new(
        request.Name,
        request.Code,
        request.Description,
        request.Price,
        request.Currency,
        request.DurationDays,
        request.MaxUsers,
        request.MaxPatients,
        request.MaxDoctors,
        request.FeaturesJson,
        request.IsActive);

    private static UpsertPlatformPlanDto ToUpsert(UpdatePlatformPlanRequest request) => new(
        request.Name,
        request.Code,
        request.Description,
        request.Price,
        request.Currency,
        request.DurationDays,
        request.MaxUsers,
        request.MaxPatients,
        request.MaxDoctors,
        request.FeaturesJson,
        request.IsActive);

    private static BaseResponse<T> Success<T>(T data, string message = "OK", int statusCode = 200) => new()
    {
        Success = statusCode < 400,
        Message = message,
        Data = data,
        StatusCode = statusCode
    };

    private static BaseResponse<T> Failure<T>(string message, int statusCode, IEnumerable<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Data = default,
        Errors = errors?.ToList() ?? [],
        StatusCode = statusCode
    };
}
