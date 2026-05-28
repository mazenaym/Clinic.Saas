using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Operations.Commands;
using Clinic.Saas.Service.UseCases.Operations.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Clinic.Saas.api.Controllers;

[Route("api/tenant")]
[ApiController]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly GetTenantStatusQuery.Handler _tenantStatus;
    private readonly GetClinicSettingsQuery.Handler _getClinicSettings;
    private readonly UpdateClinicSettingsCommand.Handler _updateClinicSettings;
    private readonly IAuditService _auditService;

    public TenantController(
        ICurrentUserService currentUser,
        GetTenantStatusQuery.Handler tenantStatus,
        GetClinicSettingsQuery.Handler getClinicSettings,
        UpdateClinicSettingsCommand.Handler updateClinicSettings,
        IAuditService auditService)
    {
        _currentUser = currentUser;
        _tenantStatus = tenantStatus;
        _getClinicSettings = getClinicSettings;
        _updateClinicSettings = updateClinicSettings;
        _auditService = auditService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _tenantStatus.Handle(new GetTenantStatusQuery.Query
        {
            TenantId = _currentUser.TenantId.Value
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getClinicSettings.Handle(new GetClinicSettingsQuery.Query
        {
            TenantId = _currentUser.TenantId.Value
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateClinicSettingsDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _updateClinicSettings.Handle(new UpdateClinicSettingsCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Settings = dto
        });

        if (result.Success)
        {
            await Audit("Update", "ClinicSettings", _currentUser.TenantId.Value, dto);
        }

        return StatusCode(result.StatusCode, result);
    }

    private async Task Audit(string action, string entityName, Guid? entityId, object? newValues)
    {
        try
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = _currentUser.TenantId,
                UserId = _currentUser.UserId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                CreatedAt = DateTime.UtcNow
            });
        }
        catch
        {
            // Audit logging must not break the user operation.
        }
    }
}
