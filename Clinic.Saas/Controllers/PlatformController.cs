using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Admin.Commands;
using Clinic.Saas.Service.UseCases.Admin.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/platform")]
[ApiController]
[Authorize(Roles = "SuperAdmin")]
public class PlatformController : ControllerBase
{
    private readonly IPlanService _plans;
    private readonly ISubscriptionService _subscriptions;
    private readonly IPlatformDashboardService _dashboard;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly CreateClinicCommand.Handler _createClinic;
    private readonly UpdateClinicCommand.Handler _updateClinic;
    private readonly GetAdminClinicByIdQuery.Handler _clinicById;
    private readonly GetActivityLogQuery.Handler _activityLog;

    public PlatformController(
        IPlanService plans,
        ISubscriptionService subscriptions,
        IPlatformDashboardService dashboard,
        ICurrentUserService currentUser,
        IAuditService audit,
        CreateClinicCommand.Handler createClinic,
        UpdateClinicCommand.Handler updateClinic,
        GetAdminClinicByIdQuery.Handler clinicById,
        GetActivityLogQuery.Handler activityLog)
    {
        _plans = plans;
        _subscriptions = subscriptions;
        _dashboard = dashboard;
        _currentUser = currentUser;
        _audit = audit;
        _createClinic = createClinic;
        _updateClinic = updateClinic;
        _clinicById = clinicById;
        _activityLog = activityLog;
    }

    [HttpGet("dashboard/summary")]
    public async Task<IActionResult> DashboardSummary()
        => OkResponse(await _dashboard.GetDashboardSummaryAsync());

    [HttpGet("plans")]
    public async Task<IActionResult> Plans([FromQuery] bool includeInactive = true)
        => OkResponse(await _plans.GetPlansAsync(includeInactive));

    [HttpGet("plans/{id:guid}")]
    public async Task<IActionResult> Plan(Guid id)
    {
        var plan = await _plans.GetPlanByIdAsync(id);
        return plan is null ? NotFoundResponse<PlatformPlanDto>("Plan was not found.") : OkResponse(plan);
    }

    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] UpsertPlatformPlanDto dto)
    {
        var plan = await _plans.CreatePlanAsync(dto);
        await LogPlatformAuditAsync("CreatePlan", "SubscriptionPlan", plan.Id, plan);
        return StatusCode(201, Success(plan, "Plan created.", 201));
    }

    [HttpPut("plans/{id:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpsertPlatformPlanDto dto)
    {
        var updated = await _plans.UpdatePlanAsync(id, dto);
        if (updated is null)
        {
            return NotFoundResponse<PlatformPlanDto>("Plan was not found.");
        }

        await LogPlatformAuditAsync("UpdatePlan", "SubscriptionPlan", id, updated);
        return OkResponse(updated, "Plan updated.");
    }

    [HttpDelete("plans/{id:guid}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        if (!await _plans.DeletePlanAsync(id))
        {
            return NotFoundResponse<bool>("Plan was not found.");
        }

        await LogPlatformAuditAsync("DeletePlan", "SubscriptionPlan", id, new { id });
        return OkResponse(true, "Plan deleted.");
    }

    [HttpPatch("plans/{id:guid}/status")]
    public async Task<IActionResult> UpdatePlanStatus(Guid id, [FromBody] UpdatePlatformPlanStatusRequest dto)
    {
        if (!await _plans.SetPlanActiveAsync(id, dto.IsActive))
        {
            return NotFoundResponse<bool>("Plan was not found.");
        }

        await LogPlatformAuditAsync(dto.IsActive ? "ActivatePlan" : "DeactivatePlan", "SubscriptionPlan", id, new { id, dto.IsActive });
        return OkResponse(true, "Plan status updated.");
    }

    [HttpPatch("plans/{id:guid}/activate")]
    public async Task<IActionResult> ActivatePlan(Guid id)
        => await _plans.SetPlanActiveAsync(id, true) ? OkResponse(true, "Plan activated.") : NotFoundResponse<bool>("Plan was not found.");

    [HttpPatch("plans/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivatePlan(Guid id)
        => await _plans.SetPlanActiveAsync(id, false) ? OkResponse(true, "Plan deactivated.") : NotFoundResponse<bool>("Plan was not found.");

    [HttpGet("clinics")]
    public async Task<IActionResult> Clinics([FromQuery] PlatformClinicFilterDto filter)
        => OkResponse(await _dashboard.GetClinicsOverviewAsync(filter));

    [HttpGet("clinics/{id:guid}")]
    public async Task<IActionResult> Clinic(Guid id)
    {
        var result = await _clinicById.Handle(new GetAdminClinicByIdQuery.Query { ClinicId = id });
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("clinics")]
    public async Task<IActionResult> CreateClinic([FromBody] CreateClinicDto dto, [FromQuery] Guid? planId = null)
    {
        var result = await _createClinic.Handle(new CreateClinicCommand.Command { Clinic = dto });
        if (result.Success && result.Data is not null)
        {
            await _subscriptions.CreateInitialSubscriptionAsync(result.Data.Id, planId, _currentUser.UserId);
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("clinics/{id:guid}")]
    public async Task<IActionResult> UpdateClinic(Guid id, [FromBody] UpdateClinicDto dto)
    {
        var result = await _updateClinic.Handle(new UpdateClinicCommand.Command { ClinicId = id, Clinic = dto });
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("clinics/{id:guid}/suspend")]
    public async Task<IActionResult> SuspendClinic(Guid id, [FromBody] SuspendTenantDto dto)
        => await _subscriptions.SuspendTenantAsync(id, dto.Reason, _currentUser.UserId)
            ? OkResponse(true, "Clinic suspended.")
            : NotFoundResponse<bool>("Clinic was not found.");

    [HttpPatch("clinics/{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateClinic(Guid id)
        => await _subscriptions.ReactivateTenantAsync(id, _currentUser.UserId)
            ? OkResponse(true, "Clinic reactivated.")
            : StatusCode(409, Success(false, "Clinic needs a valid subscription before reactivation.", 409));

    [HttpPatch("clinics/{id:guid}/disable")]
    public async Task<IActionResult> DisableClinic(Guid id)
        => await _subscriptions.SuspendTenantAsync(id, "Disabled by platform admin.", _currentUser.UserId)
            ? OkResponse(true, "Clinic disabled.")
            : NotFoundResponse<bool>("Clinic was not found.");

    [HttpGet("subscriptions")]
    public async Task<IActionResult> Subscriptions([FromQuery] PlatformSubscriptionFilterDto filter)
        => OkResponse(await _subscriptions.GetSubscriptionsAsync(filter));

    [HttpGet("subscriptions/{id:guid}")]
    public async Task<IActionResult> Subscription(Guid id)
    {
        var items = await _subscriptions.GetSubscriptionsAsync(new PlatformSubscriptionFilterDto(null, null, null, null, null, 1, 200));
        var item = items.FirstOrDefault(x => x.Id == id);
        return item is null ? NotFoundResponse<TenantSubscriptionDto>("Subscription was not found.") : OkResponse(item);
    }

    [HttpGet("clinics/{tenantId:guid}/subscription")]
    public async Task<IActionResult> CurrentSubscription(Guid tenantId)
    {
        var subscription = await _subscriptions.GetCurrentSubscriptionAsync(tenantId);
        return subscription is null ? NotFoundResponse<TenantSubscriptionDto>("Subscription was not found.") : OkResponse(subscription);
    }

    [HttpPost("clinics/{tenantId:guid}/subscription/renew")]
    public async Task<IActionResult> Renew(Guid tenantId, [FromBody] RenewTenantSubscriptionRequest dto)
    {
        var validation = ValidateRenewRequest(tenantId, dto);
        if (validation.Count > 0)
        {
            return BadRequest(new BaseResponse<TenantSubscriptionDto>
            {
                Success = false,
                Message = "Subscription renewal request is invalid.",
                Errors = validation,
                StatusCode = 400
            });
        }

        var request = dto with { TenantId = tenantId };
        var renewed = await _subscriptions.RenewSubscriptionAsync(request, _currentUser.UserId);
        return renewed is null ? NotFoundResponse<TenantSubscriptionDto>("Clinic was not found.") : OkResponse(renewed, "Subscription renewed.");
    }

    [HttpPost("clinics/{tenantId:guid}/subscription/change-plan")]
    public Task<IActionResult> ChangePlan(Guid tenantId, [FromBody] RenewTenantSubscriptionRequest dto)
        => Renew(tenantId, dto);

    [HttpPost("clinics/{tenantId:guid}/subscription/cancel")]
    public async Task<IActionResult> Cancel(Guid tenantId, [FromBody] SuspendTenantDto dto)
    {
        var current = await _subscriptions.GetCurrentSubscriptionAsync(tenantId);
        if (current is null) return NotFoundResponse<TenantSubscriptionDto>("Subscription was not found.");
        await _subscriptions.CancelSubscriptionAsync(current.Id, dto.Reason);
        return OkResponse(true, "Subscription cancelled.");
    }

    [HttpGet("subscriptions/expiring-soon")]
    public async Task<IActionResult> ExpiringSoon([FromQuery] int days = 7)
        => OkResponse(await _subscriptions.GetExpiringSoonSubscriptionsAsync(days));

    [HttpGet("subscriptions/expired")]
    public async Task<IActionResult> Expired()
        => OkResponse(await _subscriptions.GetExpiredSubscriptionsAsync());

    [HttpGet("reports/revenue")]
    public async Task<IActionResult> RevenueReport()
        => OkResponse(await _dashboard.GetDashboardSummaryAsync());

    [HttpGet("reports/subscriptions")]
    public async Task<IActionResult> SubscriptionReport([FromQuery] PlatformSubscriptionFilterDto filter)
        => OkResponse(await _subscriptions.GetSubscriptionsAsync(filter));

    [HttpGet("reports/clinics-growth")]
    public async Task<IActionResult> ClinicsGrowth([FromQuery] PlatformClinicFilterDto filter)
        => OkResponse(await _dashboard.GetClinicsOverviewAsync(filter));

    [HttpGet("reports/usage")]
    public async Task<IActionResult> Usage([FromQuery] PlatformClinicFilterDto filter)
        => OkResponse(await _dashboard.GetClinicsOverviewAsync(filter));

    [HttpGet("reports/platform")]
    public async Task<IActionResult> PlatformReports([FromQuery] PlatformReportsFilterDto filter)
        => OkResponse(await _dashboard.GetReportsAsync(filter));

    [HttpGet("settings/platform")]
    public async Task<IActionResult> PlatformSettings()
        => OkResponse(await _dashboard.GetSettingsAsync());

    [HttpPut("settings/platform")]
    public async Task<IActionResult> UpdatePlatformSettings([FromBody] PlatformSettingsDto dto)
    {
        var validation = ValidateSettings(dto);
        if (validation.Count > 0)
        {
            return BadRequest(new BaseResponse<PlatformSettingsDto>
            {
                Success = false,
                Message = "Platform settings request is invalid.",
                Errors = validation,
                StatusCode = 400
            });
        }

        return OkResponse(await _dashboard.UpdateSettingsAsync(dto, _currentUser.UserId), "Platform settings updated.");
    }

    [HttpPost("subscriptions/check-expiry")]
    public async Task<IActionResult> CheckExpiry()
        => OkResponse(await _subscriptions.CheckAndExpireSubscriptionsAsync(), "Subscription expiry check completed.");

    [HttpGet("audit-logs")]
    public async Task<IActionResult> AuditLogs([FromQuery] int pageSize = 100)
    {
        var result = await _activityLog.Handle(new GetActivityLogQuery.Query
        {
            Take = pageSize,
            IncludeAllTenants = true
        });
        return StatusCode(result.StatusCode, result);
    }

    private static IActionResult OkResponse<T>(T data, string message = "OK") => new OkObjectResult(Success(data, message, 200));

    private static IActionResult NotFoundResponse<T>(string message) => new NotFoundObjectResult(Success<T?>(default, message, 404));

    private static BaseResponse<T> Success<T>(T data, string message, int statusCode) => new()
    {
        Success = statusCode < 400,
        Message = message,
        Data = data,
        StatusCode = statusCode
    };

    private Task LogPlatformAuditAsync(string action, string entityName, Guid entityId, object values)
    {
        return _audit.LogAsync(new AuditEntry
        {
            UserId = _currentUser.UserId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            NewValues = System.Text.Json.JsonSerializer.Serialize(values),
            CreatedAt = DateTime.UtcNow
        });
    }

    private static List<string> ValidateRenewRequest(Guid tenantId, RenewTenantSubscriptionRequest dto)
    {
        var errors = new List<string>();
        if (tenantId == Guid.Empty)
        {
            errors.Add("TenantId is required.");
        }

        if (dto.PlanId == Guid.Empty)
        {
            errors.Add("PlanId is required.");
        }

        if (dto.ActualPaidAmount is < 0)
        {
            errors.Add("ActualPaidAmount must be greater than or equal to 0.");
        }

        if (!string.IsNullOrWhiteSpace(dto.PaymentMethod) && dto.PaymentMethod.Length > 100)
        {
            errors.Add("PaymentMethod must be 100 characters or fewer.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > 500)
        {
            errors.Add("Notes must be 500 characters or fewer.");
        }

        return errors;
    }

    private static List<string> ValidateSettings(PlatformSettingsDto dto)
    {
        var errors = new List<string>();
        if (dto.TrialDurationDays < 0) errors.Add("TrialDurationDays must be greater than or equal to 0.");
        if (dto.ExpiringSoonThresholdDays < 0) errors.Add("ExpiringSoonThresholdDays must be greater than or equal to 0.");
        if (dto.DefaultGracePeriodDays < 0) errors.Add("DefaultGracePeriodDays must be greater than or equal to 0.");
        if (string.IsNullOrWhiteSpace(dto.CurrencyCode) || dto.CurrencyCode.Length > 10) errors.Add("CurrencyCode is required and must be 10 characters or fewer.");
        if (!string.IsNullOrWhiteSpace(dto.PlatformSupportEmail) && dto.PlatformSupportEmail.Length > 200) errors.Add("PlatformSupportEmail must be 200 characters or fewer.");
        if (!string.IsNullOrWhiteSpace(dto.PlatformSupportPhone) && dto.PlatformSupportPhone.Length > 50) errors.Add("PlatformSupportPhone must be 50 characters or fewer.");
        if (!string.IsNullOrWhiteSpace(dto.PaymentMethodsEnabled) && dto.PaymentMethodsEnabled.Length > 1000) errors.Add("PaymentMethodsEnabled must be 1000 characters or fewer.");
        if (dto.TaxPercentage is < 0) errors.Add("TaxPercentage must be greater than or equal to 0.");
        return errors;
    }
}
