using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Obsolete("Compatibility API. Use /api/platform endpoints.")]
public sealed class AdminController(IPlatformDashboardFacade dashboard, IPlatformClinicsFacade clinics) : LegacyCompatibilityControllerBase
{
    [AllowAnonymous]
    [HttpPost("bootstrap")]
    public async Task<IActionResult> Bootstrap([FromBody] BootstrapSuperAdminDto dto)
    {
        var result = await clinics.BootstrapAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        AddSuccessor("/api/platform/dashboard/summary");
        var result = await dashboard.GetLegacySummaryAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("clinics")]
    public async Task<IActionResult> GetClinics()
    {
        AddSuccessor("/api/platform/clinics");
        var result = await clinics.GetLegacyListAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("clinics/{clinicId:guid}")]
    public async Task<IActionResult> GetClinic(Guid clinicId)
    {
        AddSuccessor($"/api/platform/clinics/{clinicId}");
        var result = await clinics.GetAsync(clinicId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("clinics")]
    public async Task<IActionResult> CreateClinic([FromBody] CreateClinicDto dto)
    {
        AddSuccessor("/api/platform/clinics");
        var result = await clinics.CreateAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("clinics/{clinicId:guid}")]
    public async Task<IActionResult> UpdateClinic(Guid clinicId, [FromBody] UpdateClinicDto dto)
    {
        AddSuccessor($"/api/platform/clinics/{clinicId}");
        var result = await clinics.UpdateAsync(clinicId, dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("clinics/{clinicId:guid}/status")]
    public async Task<IActionResult> SetClinicStatus(Guid clinicId, [FromBody] SetClinicStatusDto dto)
    {
        AddSuccessor($"/api/platform/clinics/{clinicId}");
        var result = await clinics.SetLegacyStatusAsync(clinicId, dto.IsActive);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("clinics/{clinicId:guid}/subscriptions")]
    public async Task<IActionResult> AddSubscription(Guid clinicId, [FromBody] CreateSubscriptionDto dto)
    {
        AddSuccessor($"/api/platform/clinics/{clinicId}/subscription/renew");
        var result = await clinics.CreateLegacySubscriptionAsync(clinicId, dto);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class SetClinicStatusDto { public bool IsActive { get; set; } }
