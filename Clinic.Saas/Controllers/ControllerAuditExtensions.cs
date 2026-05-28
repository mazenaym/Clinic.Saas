using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Clinic.Saas.api.Controllers;

public static class ControllerAuditExtensions
{
    public static Task AuditAsync(
        this ControllerBase controller,
        IAuditService auditService,
        ICurrentUserService currentUser,
        string action,
        string entityName,
        Guid? entityId,
        object? summary = null)
    {
        return auditService.LogAsync(new AuditEntry
        {
            TenantId = currentUser.TenantId,
            UserId = currentUser.UserId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            NewValues = summary is null ? null : JsonSerializer.Serialize(summary),
            IpAddress = controller.HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = controller.Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.UtcNow
        });
    }
}
