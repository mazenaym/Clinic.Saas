using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[ApiController]
[Route("api/media")]
[Authorize]
public sealed class MediaController(
    ICurrentUserService currentUser,
    IUserRepository users,
    ITenantRepository tenants,
    IFileStorageService storage,
    IAuditService audit) : ControllerBase
{
    private const long MaxImageBytes = 8 * 1024 * 1024;

    [HttpPost("me/avatar")]
    [RequestSizeLimit(9 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (!currentUser.TenantId.HasValue || !currentUser.UserId.HasValue) return Unauthorized();
        if (file is null || file.Length == 0) return BadRequest(Fail("Image is required."));
        if (file.Length > MaxImageBytes) return StatusCode(413, Fail("Image is too large."));
        var existing = await users.GetByIdAsync(currentUser.TenantId.Value, currentUser.UserId.Value);
        if (existing is null) return NotFound(Fail("User not found."));
        try
        {
            await using var input = file.OpenReadStream();
            var saved = await storage.SaveImageAsync(currentUser.TenantId.Value, "avatars", currentUser.UserId.Value, file.FileName, input);
            if (!await users.UpdatePreferencesAsync(currentUser.TenantId.Value, currentUser.UserId.Value, saved.StorageKey))
            {
                await storage.DeleteAsync(saved.StorageKey);
                return Conflict(Fail("Avatar could not be saved."));
            }
            if (!string.IsNullOrWhiteSpace(existing.AvatarUrl)) await storage.DeleteAsync(existing.AvatarUrl);
            await this.AuditAsync(audit, currentUser, "Upload", "UserAvatar", currentUser.UserId, new { saved.Length });
            return Ok(new BaseResponse<object> { Success = true, Data = new { url = "/api/media/me/avatar", saved.Length }, Message = "Avatar uploaded.", StatusCode = 200 });
        }
        catch (InvalidOperationException ex) { return BadRequest(Fail(ex.Message)); }
    }

    [HttpGet("me/avatar")]
    public async Task<IActionResult> Avatar()
    {
        if (!currentUser.TenantId.HasValue || !currentUser.UserId.HasValue) return Unauthorized();
        var user = await users.GetByIdAsync(currentUser.TenantId.Value, currentUser.UserId.Value);
        return await StoredImage(user?.AvatarUrl);
    }

    [Authorize(Roles = "Admin", Policy = Permissions.SettingsManage)]
    [HttpPost("tenant/logo")]
    [RequestSizeLimit(9 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        if (!currentUser.TenantId.HasValue) return Unauthorized();
        if (file is null || file.Length == 0) return BadRequest(Fail("Image is required."));
        if (file.Length > MaxImageBytes) return StatusCode(413, Fail("Image is too large."));
        var tenant = await tenants.GetByIdAsync(currentUser.TenantId.Value);
        if (tenant is null) return NotFound(Fail("Clinic not found."));
        try
        {
            await using var input = file.OpenReadStream();
            var saved = await storage.SaveImageAsync(tenant.Id, "logos", tenant.Id, file.FileName, input);
            var old = tenant.LogoUrl;
            tenant.LogoUrl = saved.StorageKey;
            tenant.UpdatedAt = DateTime.UtcNow;
            await tenants.UpdateAsync(tenant);
            if (!string.IsNullOrWhiteSpace(old)) await storage.DeleteAsync(old);
            await this.AuditAsync(audit, currentUser, "Upload", "ClinicLogo", tenant.Id, new { saved.Length });
            return Ok(new BaseResponse<object> { Success = true, Data = new { url = "/api/media/tenant/logo", saved.Length }, Message = "Logo uploaded.", StatusCode = 200 });
        }
        catch (InvalidOperationException ex) { return BadRequest(Fail(ex.Message)); }
    }

    [HttpGet("tenant/logo")]
    public async Task<IActionResult> Logo()
    {
        if (!currentUser.TenantId.HasValue) return Unauthorized();
        var tenant = await tenants.GetByIdAsync(currentUser.TenantId.Value);
        return await StoredImage(tenant?.LogoUrl);
    }

    private async Task<IActionResult> StoredImage(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return NotFound();
        var stream = await storage.OpenReadAsync(key);
        if (stream is null) return NotFound();
        Response.Headers.CacheControl = "private,max-age=300";
        return File(stream, "image/webp", enableRangeProcessing: true);
    }

    private static BaseResponse<object> Fail(string message) => new() { Success = false, Message = message, Errors = [message], StatusCode = 400 };
}
