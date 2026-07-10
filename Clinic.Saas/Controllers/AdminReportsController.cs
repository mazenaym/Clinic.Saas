using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize]
[Obsolete("Compatibility API. Use /api/platform/reports or /api/platform/audit-logs endpoints.")]
public sealed class AdminReportsController(IPlatformReportsFacade reports, IPlatformAuditLogsFacade auditLogs, ICurrentUserService currentUser) : LegacyCompatibilityControllerBase
{
    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("usage")]
    public async Task<IActionResult> Usage() { AddSuccessor("/api/platform/reports/usage"); var result = await reports.GetLegacyUsageAsync(); return StatusCode(result.StatusCode, result); }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("subscription-revenue")]
    public async Task<IActionResult> SubscriptionRevenue() { AddSuccessor("/api/platform/reports/revenue"); var result = await reports.GetLegacyRevenueAsync(); return StatusCode(result.StatusCode, result); }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("expiring-subscriptions")]
    public async Task<IActionResult> ExpiringSubscriptions([FromQuery] int days = 14) { AddSuccessor("/api/platform/subscriptions/expiring-soon"); var result = await reports.GetLegacyExpiringAsync(days); return StatusCode(result.StatusCode, result); }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet("activity-log")]
    public async Task<IActionResult> ActivityLog([FromQuery] int take = 100)
    {
        AddSuccessor("/api/platform/audit-logs");
        if (currentUser.Role != UserRole.SuperAdmin && !currentUser.TenantId.HasValue) return Unauthorized();
        var result = await auditLogs.GetAsync(take, currentUser.Role == UserRole.SuperAdmin, currentUser.TenantId);
        return StatusCode(result.StatusCode, result);
    }
}
