using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.Security;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Clinic.Saas.api.Authorization;

public static class PermissionPolicies
{
    public static AuthorizationOptions AddClinicPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (var permission in Permissions.All)
        {
            options.AddPolicy(permission, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context => HasPermission(context.User, permission));
            });
        }

        return options;
    }

    private static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        var role = user.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<UserRole>(role, true, out var parsedRole) &&
               Permissions.RolePermissions.TryGetValue(parsedRole, out var permissions) &&
               permissions.Contains(permission);
    }
}
