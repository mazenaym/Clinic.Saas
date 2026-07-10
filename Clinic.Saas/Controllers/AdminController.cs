using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Cryptography;
using System.Text;

namespace Clinic.Saas.api.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Obsolete("Compatibility API. Use /api/platform endpoints.")]
public sealed class AdminController(
    IPlatformDashboardFacade dashboard,
    IPlatformClinicsFacade clinics,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<AdminController> logger) : LegacyCompatibilityControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("bootstrap")]
    [HttpPost("bootstrap")]
    public async Task<IActionResult> Bootstrap([FromBody] BootstrapSuperAdminDto dto, [FromHeader(Name = "X-ClinicFlow-Setup-Key")] string? setupKey)
    {
        var enabled = configuration.GetValue<bool>("Bootstrap:Enabled") || environment.IsDevelopment();
        if (!enabled)
        {
            logger.LogWarning("Blocked SuperAdmin bootstrap attempt because bootstrap is disabled");
            return StatusCode(403, new BaseResponse<AdminClinicDto> { Success = false, Message = "Bootstrap is disabled.", StatusCode = 403 });
        }
        var expected = configuration["Bootstrap:SetupKey"];
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(setupKey) || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(setupKey))))
        {
            logger.LogWarning("Rejected SuperAdmin bootstrap attempt due to an invalid setup key");
            return Unauthorized(new BaseResponse<AdminClinicDto> { Success = false, Message = "A valid setup key is required.", StatusCode = 401 });
        }
        var result = await clinics.BootstrapAsync(dto);
        if (result.StatusCode == 409) logger.LogWarning("Rejected SuperAdmin bootstrap attempt because a SuperAdmin already exists");
        else if (result.Success) logger.LogInformation("SuperAdmin bootstrap completed successfully");
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        AddSuccessor("/api/platform/dashboard/summary");
        var value = await dashboard.GetSummaryAsync();
        return Ok(new BaseResponse<AdminDashboardStatsDto> { Success = true, Message = "Admin dashboard loaded successfully", Data = Map(value), StatusCode = 200 });
    }

    [HttpGet("clinics")]
    public async Task<IActionResult> GetClinics()
    {
        AddSuccessor("/api/platform/clinics");
        var rows = await clinics.GetAsync(new PlatformClinicFilterDto(null, null, null, null, null, null, 1, 200));
        return Ok(new BaseResponse<IEnumerable<AdminClinicDto>> { Success = true, Message = "Admin clinics loaded successfully", Data = rows, StatusCode = 200 });
    }

    [HttpGet("clinics/{clinicId:guid}")]
    public async Task<IActionResult> GetClinic(Guid clinicId)
    {
        AddSuccessor($"/api/platform/clinics/{clinicId}");
        var result = await clinics.GetByIdAsync(clinicId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("clinics")]
    public async Task<IActionResult> CreateClinic([FromBody] CreateClinicDto dto)
    {
        AddSuccessor("/api/platform/clinics");
        var result = await clinics.CreateWithInitialSubscriptionAsync(dto, null, null);
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

    private static AdminDashboardStatsDto Map(PlatformDashboardSummaryDto value) => new()
    {
        TotalClinics = value.TotalClinics,
        ActiveClinics = value.ActiveClinics,
        InactiveClinics = value.TotalClinics - value.ActiveClinics,
        TotalUsers = value.TotalUsers,
        TotalPatients = value.TotalPatients,
        ActiveSubscriptions = value.ActiveClinics,
        TrialSubscriptions = value.TrialClinics,
        ExpiredSubscriptions = value.ExpiredSubscriptionsCount,
        TotalRevenue = value.AnnualSubscriptionRevenue,
        MonthlyRevenue = value.MonthlySubscriptionRevenue,
        TodayRevenue = 0,
        RecentClinics = value.RecentClinics.ToList()
    };
}

public sealed class SetClinicStatusDto { public bool IsActive { get; set; } }
