using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[ApiController]
[Route("api/platform/settings")]
[Authorize(Roles = "SuperAdmin")]
public sealed class PlatformSettingsController(IPlatformDashboardService dashboard, ICurrentUserService currentUser) : PlatformControllerBase
{
    [HttpGet("platform")]
    public async Task<IActionResult> Get() => OkResponse(await dashboard.GetSettingsAsync());

    [HttpPut("platform")]
    public async Task<IActionResult> Update([FromBody] PlatformSettingsDto dto)
    {
        var errors = Validate(dto);
        if (errors.Count > 0) return BadRequest(new BaseResponse<PlatformSettingsDto> { Success = false, Message = "Platform settings request is invalid.", Errors = errors, StatusCode = 400 });
        return OkResponse(await dashboard.UpdateSettingsAsync(dto, currentUser.UserId), "Platform settings updated.");
    }

    private static List<string> Validate(PlatformSettingsDto dto)
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
