using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Clinic.Saas.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId => TryGetGuid(ClaimTypes.NameIdentifier);
    public Guid? TenantId => TryGetGuid("tenant_id") ?? TryGetTenantIdFromRequest();
    public string? Email => User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue(ClaimTypes.Name);
    public UserRole? Role
    {
        get
        {
            var role = User?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(role, true, out var parsed) ? parsed : null;
        }
    }
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    private Guid? TryGetGuid(string claimType)
    {
        var value = User?.FindFirstValue(claimType);
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private Guid? TryGetTenantIdFromRequest()
    {
        var items = _httpContextAccessor.HttpContext?.Items;
        if (items is null || !items.TryGetValue("TenantId", out var value))
        {
            return null;
        }

        return value switch
        {
            Guid tenantId => tenantId,
            string text when Guid.TryParse(text, out var tenantId) => tenantId,
            _ => null
        };
    }
}
