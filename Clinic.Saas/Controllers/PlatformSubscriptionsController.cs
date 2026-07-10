using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[ApiController]
[Route("api/platform/subscriptions")]
[Authorize(Roles = "SuperAdmin")]
public sealed class PlatformSubscriptionsController(ISubscriptionService subscriptions, ICurrentUserService currentUser) : PlatformControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PlatformSubscriptionFilterDto filter) => OkResponse(await subscriptions.GetSubscriptionsAsync(filter));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var items = await subscriptions.GetSubscriptionsAsync(new PlatformSubscriptionFilterDto(null, null, null, null, null, 1, 200));
        return items.FirstOrDefault(x => x.Id == id) is { } item ? OkResponse(item) : NotFoundResponse<TenantSubscriptionDto>("Subscription was not found.");
    }

    [HttpGet("expiring-soon")]
    public async Task<IActionResult> ExpiringSoon([FromQuery] int days = 7) => OkResponse(await subscriptions.GetExpiringSoonSubscriptionsAsync(days));

    [HttpGet("expired")]
    public async Task<IActionResult> Expired() => OkResponse(await subscriptions.GetExpiredSubscriptionsAsync());

    [HttpPost("check-expiry")]
    public async Task<IActionResult> CheckExpiry() => OkResponse(await subscriptions.CheckAndExpireSubscriptionsAsync(), "Subscription expiry check completed.");

    [HttpGet("~/api/platform/clinics/{tenantId:guid}/subscription")]
    public async Task<IActionResult> Current(Guid tenantId) => await subscriptions.GetCurrentSubscriptionAsync(tenantId) is { } item ? OkResponse(item) : NotFoundResponse<TenantSubscriptionDto>("Subscription was not found.");

    [HttpPost("~/api/platform/clinics/{tenantId:guid}/subscription/renew")]
    public async Task<IActionResult> Renew(Guid tenantId, [FromBody] RenewTenantSubscriptionRequest dto)
    {
        var errors = Validate(tenantId, dto);
        if (errors.Count > 0) return BadRequest(new BaseResponse<TenantSubscriptionDto> { Success = false, Message = "Subscription renewal request is invalid.", Errors = errors, StatusCode = 400 });
        var renewed = await subscriptions.RenewSubscriptionAsync(dto with { TenantId = tenantId }, currentUser.UserId);
        return renewed is null ? NotFoundResponse<TenantSubscriptionDto>("Clinic was not found.") : OkResponse(renewed, "Subscription renewed.");
    }

    [HttpPost("~/api/platform/clinics/{tenantId:guid}/subscription/change-plan")]
    public Task<IActionResult> ChangePlan(Guid tenantId, [FromBody] RenewTenantSubscriptionRequest dto) => Renew(tenantId, dto);

    [HttpPost("~/api/platform/clinics/{tenantId:guid}/subscription/cancel")]
    public async Task<IActionResult> Cancel(Guid tenantId, [FromBody] SuspendTenantDto dto)
    {
        var current = await subscriptions.GetCurrentSubscriptionAsync(tenantId);
        if (current is null) return NotFoundResponse<TenantSubscriptionDto>("Subscription was not found.");
        await subscriptions.CancelSubscriptionAsync(current.Id, dto.Reason);
        return OkResponse(true, "Subscription cancelled.");
    }

    private static List<string> Validate(Guid tenantId, RenewTenantSubscriptionRequest dto)
    {
        var errors = new List<string>();
        if (tenantId == Guid.Empty) errors.Add("TenantId is required.");
        if (dto.PlanId == Guid.Empty) errors.Add("PlanId is required.");
        if (dto.ActualPaidAmount is < 0) errors.Add("ActualPaidAmount must be greater than or equal to 0.");
        if (!string.IsNullOrWhiteSpace(dto.PaymentMethod) && dto.PaymentMethod.Length > 100) errors.Add("PaymentMethod must be 100 characters or fewer.");
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > 500) errors.Add("Notes must be 500 characters or fewer.");
        return errors;
    }
}
