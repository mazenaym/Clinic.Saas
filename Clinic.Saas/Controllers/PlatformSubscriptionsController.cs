using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[ApiController]
[Route("api/platform/subscriptions")]
[Authorize(Roles = "SuperAdmin")]
public sealed class PlatformSubscriptionsController(ISubscriptionService subscriptions, IPlatformSubscriptionsFacade facade, ICurrentUserService currentUser) : PlatformControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PlatformSubscriptionFilterDto filter) => OkResponse(await subscriptions.GetSubscriptionsAsync(filter));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        return await facade.GetByIdAsync(id) is { } item ? OkResponse(item) : NotFoundResponse<TenantSubscriptionDto>("Subscription was not found.");
    }

    [HttpGet("expiring-soon")]
    public async Task<IActionResult> ExpiringSoon([FromQuery] int days = 7) => OkResponse(await facade.GetExpiringAsync(days));

    [HttpGet("expired")]
    public async Task<IActionResult> Expired() => OkResponse(await subscriptions.GetExpiredSubscriptionsAsync());

    [HttpPost("check-expiry")]
    public async Task<IActionResult> CheckExpiry() => OkResponse(await subscriptions.CheckAndExpireSubscriptionsAsync(), "Subscription expiry check completed.");

    [HttpGet("~/api/platform/clinics/{tenantId:guid}/subscription")]
    public async Task<IActionResult> Current(Guid tenantId) => await subscriptions.GetCurrentSubscriptionAsync(tenantId) is { } item ? OkResponse(item) : NotFoundResponse<TenantSubscriptionDto>("Subscription was not found.");

    [HttpPost("~/api/platform/clinics/{tenantId:guid}/subscription/renew")]
    public async Task<IActionResult> Renew(Guid tenantId, [FromBody] RenewTenantSubscriptionRequest dto)
    {
        var result = await facade.RenewAsync(tenantId, dto, currentUser.UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("~/api/platform/clinics/{tenantId:guid}/subscription/change-plan")]
    public async Task<IActionResult> ChangePlan(Guid tenantId, [FromBody] RenewTenantSubscriptionRequest dto)
    {
        var result = await facade.ChangePlanAsync(tenantId, dto, currentUser.UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("~/api/platform/clinics/{tenantId:guid}/subscription/cancel")]
    public async Task<IActionResult> Cancel(Guid tenantId, [FromBody] SuspendTenantDto dto)
    {
        var current = await subscriptions.GetCurrentSubscriptionAsync(tenantId);
        if (current is null) return NotFoundResponse<TenantSubscriptionDto>("Subscription was not found.");
        await subscriptions.CancelSubscriptionAsync(current.Id, dto.Reason, currentUser.UserId);
        return OkResponse(true, "Subscription cancelled.");
    }

}
