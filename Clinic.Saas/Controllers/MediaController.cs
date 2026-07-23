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
        if (file.Length > MaxImageBytes) return StatusCode(413, Fail("Image is too large.", 413));
        var existing = await users.GetByIdAsync(currentUser.TenantId.Value, currentUser.UserId.Value);
        if (existing is null) return NotFound(Fail("User not found.", 404));
        var oldAvatarKey = existing.AvatarUrl;
        StoredImage saved;
        try
        {
            await using var input = file.OpenReadStream();
            saved = await storage.SaveImageAsync(currentUser.TenantId.Value, "avatars", currentUser.UserId.Value, file.FileName, input);
        }
        catch (InvalidOperationException) { return BadRequest(Fail("The uploaded image is not supported or is invalid.")); }
        try
        {
            if (!await users.UpdatePreferencesAsync(currentUser.TenantId.Value, currentUser.UserId.Value, saved.StorageKey))
            {
                await storage.DeleteAsync(saved.StorageKey);
                return Conflict(Fail("Avatar could not be saved.", 409));
            }
        }
        catch
        {
            await storage.DeleteAsync(saved.StorageKey);
            return StatusCode(500, Fail("Avatar could not be saved.", 500));
        }
        var oldCleaned = await CleanupReplacedAsync(oldAvatarKey);
        await this.AuditAsync(audit, currentUser, "Upload", "UserAvatar", currentUser.UserId, new { saved.Length, oldCleaned });
        return Ok(new BaseResponse<object> { Success = true, Data = new { url = "/api/media/me/avatar", saved.Length }, Message = "Avatar uploaded.", StatusCode = 200 });
    }

    [HttpGet("me/avatar")]
    public async Task<IActionResult> Avatar()
    {
        if (!currentUser.TenantId.HasValue || !currentUser.UserId.HasValue) return Unauthorized();
        var user = await users.GetByIdAsync(currentUser.TenantId.Value, currentUser.UserId.Value);
        return await StoredImage(user?.AvatarUrl);
    }

    [HttpDelete("me/avatar")]
    public async Task<IActionResult> DeleteAvatar()
    {
        if (!currentUser.TenantId.HasValue || !currentUser.UserId.HasValue) return Unauthorized();
        var user = await users.GetByIdAsync(currentUser.TenantId.Value, currentUser.UserId.Value);
        if (user is null) return NotFound(Fail("User not found.", 404));
        var token = await StageExistingAsync(user.AvatarUrl);
        try
        {
            if (!await users.ClearAvatarAsync(currentUser.TenantId.Value, currentUser.UserId.Value))
            {
                if (token is not null) await storage.RestoreStagedDeleteAsync(token);
                return Conflict(Fail("Avatar could not be deleted.", 409));
            }
        }
        catch
        {
            if (token is not null) await storage.RestoreStagedDeleteAsync(token);
            return StatusCode(500, Fail("Avatar could not be deleted.", 500));
        }
        if (token is not null) await storage.CommitStagedDeleteAsync(token);
        await this.AuditAsync(audit, currentUser, "Delete", "UserAvatar", currentUser.UserId, null);
        return Ok(new BaseResponse<bool> { Success = true, Data = true, Message = "Avatar deleted.", StatusCode = 200 });
    }

    [Authorize(Roles = "Admin", Policy = Permissions.SettingsManage)]
    [HttpPost("tenant/logo")]
    [RequestSizeLimit(9 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        if (!currentUser.TenantId.HasValue) return Unauthorized();
        if (file is null || file.Length == 0) return BadRequest(Fail("Image is required."));
        if (file.Length > MaxImageBytes) return StatusCode(413, Fail("Image is too large.", 413));
        var tenant = await tenants.GetByIdAsync(currentUser.TenantId.Value);
        if (tenant is null) return NotFound(Fail("Clinic not found.", 404));
        StoredImage saved;
        try
        {
            await using var input = file.OpenReadStream();
            saved = await storage.SaveImageAsync(tenant.Id, "logos", tenant.Id, file.FileName, input);
        }
        catch (InvalidOperationException) { return BadRequest(Fail("The uploaded image is not supported or is invalid.")); }
        var old = tenant.LogoUrl;
        try
        {
            tenant.LogoUrl = saved.StorageKey;
            tenant.UpdatedAt = DateTime.UtcNow;
            await tenants.UpdateAsync(tenant);
        }
        catch
        {
            await storage.DeleteAsync(saved.StorageKey);
            return StatusCode(500, Fail("Clinic logo could not be saved.", 500));
        }
        var oldCleaned = await CleanupReplacedAsync(old);
        await this.AuditAsync(audit, currentUser, "Upload", "ClinicLogo", tenant.Id, new { saved.Length, oldCleaned });
        return Ok(new BaseResponse<object> { Success = true, Data = new { url = "/api/media/tenant/logo", saved.Length }, Message = "Logo uploaded.", StatusCode = 200 });
    }

    [HttpGet("tenant/logo")]
    public async Task<IActionResult> Logo()
    {
        if (!currentUser.TenantId.HasValue) return Unauthorized();
        var tenant = await tenants.GetByIdAsync(currentUser.TenantId.Value);
        return await StoredImage(tenant?.LogoUrl);
    }

    [Authorize(Roles = "Admin", Policy = Permissions.SettingsManage)]
    [HttpDelete("tenant/logo")]
    public async Task<IActionResult> DeleteLogo()
    {
        if (!currentUser.TenantId.HasValue) return Unauthorized();
        var tenant = await tenants.GetByIdAsync(currentUser.TenantId.Value);
        if (tenant is null) return NotFound(Fail("Clinic not found.", 404));
        var token = await StageExistingAsync(tenant.LogoUrl);
        try
        {
            tenant.LogoUrl = null;
            tenant.UpdatedAt = DateTime.UtcNow;
            await tenants.UpdateAsync(tenant);
        }
        catch
        {
            if (token is not null) await storage.RestoreStagedDeleteAsync(token);
            return StatusCode(500, Fail("Clinic logo could not be deleted.", 500));
        }
        if (token is not null) await storage.CommitStagedDeleteAsync(token);
        await this.AuditAsync(audit, currentUser, "Delete", "ClinicLogo", tenant.Id, null);
        return Ok(new BaseResponse<bool> { Success = true, Data = true, Message = "Clinic logo deleted.", StatusCode = 200 });
    }

    private async Task<IActionResult> StoredImage(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return NotFound();
        var stream = await storage.OpenReadAsync(key);
        if (stream is null) return NotFound();
        Response.Headers.CacheControl = "private,max-age=300";
        return File(stream, "image/webp", enableRangeProcessing: true);
    }

    private static BaseResponse<object> Fail(string message, int statusCode = 400) => new() { Success = false, Message = message, Errors = [message], StatusCode = statusCode };

    private async Task<string?> StageExistingAsync(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        try { return await storage.StageDeleteAsync(key); }
        catch { return null; }
    }

    private async Task<bool> CleanupReplacedAsync(string? oldKey)
    {
        if (string.IsNullOrWhiteSpace(oldKey)) return true;
        try
        {
            var token = await storage.StageDeleteAsync(oldKey);
            return token is null || await storage.CommitStagedDeleteAsync(token);
        }
        catch { return false; }
    }
}
