using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Admin.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize]
[Obsolete("Compatibility API. Use /api/platform/reports or /api/platform/audit-logs endpoints.")]
public class AdminReportsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly GetClinicUsageMetricsQuery.Handler _usageMetrics;
    private readonly GetSubscriptionRevenueReportQuery.Handler _subscriptionRevenue;
    private readonly GetExpiringSubscriptionsReportQuery.Handler _expiringSubscriptions;
    private readonly GetActivityLogQuery.Handler _activityLog;

    public AdminReportsController(
        ICurrentUserService currentUser,
        GetClinicUsageMetricsQuery.Handler usageMetrics,
        GetSubscriptionRevenueReportQuery.Handler subscriptionRevenue,
        GetExpiringSubscriptionsReportQuery.Handler expiringSubscriptions,
        GetActivityLogQuery.Handler activityLog)
    {
        _currentUser = currentUser;
        _usageMetrics = usageMetrics;
        _subscriptionRevenue = subscriptionRevenue;
        _expiringSubscriptions = expiringSubscriptions;
        _activityLog = activityLog;
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("usage")]
    public async Task<IActionResult> Usage()
    {
        var result = await _usageMetrics.Handle();
        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("subscription-revenue")]
    public async Task<IActionResult> SubscriptionRevenue()
    {
        var result = await _subscriptionRevenue.Handle();
        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("expiring-subscriptions")]
    public async Task<IActionResult> ExpiringSubscriptions([FromQuery] int days = 14)
    {
        var result = await _expiringSubscriptions.Handle(new GetExpiringSubscriptionsReportQuery.Query
        {
            Days = days
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet("activity-log")]
    public async Task<IActionResult> ActivityLog([FromQuery] int take = 100)
    {
        if (_currentUser.Role != UserRole.SuperAdmin && !_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _activityLog.Handle(new GetActivityLogQuery.Query
        {
            Take = take,
            IncludeAllTenants = _currentUser.Role == UserRole.SuperAdmin,
            TenantId = _currentUser.TenantId
        });

        return StatusCode(result.StatusCode, result);
    }
}
