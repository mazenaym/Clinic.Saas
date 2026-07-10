using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize]
[Obsolete("Compatibility API. Use /api/platform/reports or /api/platform/audit-logs endpoints.")]
public sealed class AdminReportsController(IPlatformReportsFacade reports, IPlatformSubscriptionsFacade subscriptions, IPlatformAuditLogsFacade auditLogs, ICurrentUserService currentUser) : LegacyCompatibilityControllerBase
{
    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("usage")]
    public async Task<IActionResult> Usage() { AddSuccessor("/api/platform/reports/usage"); var rows = await reports.GetClinicsAsync(new Clinic.Saas.Service.DTOs.PlatformClinicFilterDto(null, null, null, null, null, null, 1, 200)); return Ok(new Clinic.Saas.Service.DTOs.BaseResponse<List<Clinic.Saas.Service.DTOs.ClinicUsageMetricDto>> { Success = true, Message = "OK", StatusCode = 200, Data = rows.Select(x => new Clinic.Saas.Service.DTOs.ClinicUsageMetricDto { Id = x.Id, Name = x.Name, Subdomain = x.Subdomain, UsersCount = x.UsersCount, PatientsCount = x.PatientsCount, AppointmentsCount = x.AppointmentsCount }).ToList() }); }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("subscription-revenue")]
    public async Task<IActionResult> SubscriptionRevenue() { AddSuccessor("/api/platform/reports/revenue"); var value = await reports.GetRevenueAsync(); var now = DateTime.UtcNow; return Ok(new Clinic.Saas.Service.DTOs.BaseResponse<List<Clinic.Saas.Service.DTOs.SubscriptionRevenueDto>> { Success = true, Message = "OK", StatusCode = 200, Data = [new() { Year = now.Year, Month = now.Month, Revenue = value.MonthlySubscriptionRevenue, SubscriptionCount = value.ActiveClinics }] }); }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("expiring-subscriptions")]
    public async Task<IActionResult> ExpiringSubscriptions([FromQuery] int days = 14) { AddSuccessor("/api/platform/subscriptions/expiring-soon"); var rows = await subscriptions.GetExpiringAsync(days); return Ok(new Clinic.Saas.Service.DTOs.BaseResponse<List<Clinic.Saas.Service.DTOs.ExpiringSubscriptionDto>> { Success = true, Message = "OK", StatusCode = 200, Data = rows.Select(x => new Clinic.Saas.Service.DTOs.ExpiringSubscriptionDto { Name = x.TenantName, Plan = x.PlanName, EndDate = x.EndsAtUtc, Status = x.Status.ToString() }).ToList() }); }

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
