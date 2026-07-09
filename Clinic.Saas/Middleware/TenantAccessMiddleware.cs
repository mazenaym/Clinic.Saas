using Clinic.Saas.Infrastructure.Data;
using Dapper;
using System.Security.Claims;

namespace Clinic.Saas.api.Middleware;

public class TenantAccessMiddleware
{
    private static readonly PathString[] ExcludedPrefixes =
    [
        "/api/auth",
        "/api/onboarding",
        "/api/admin",
        "/api/platform",
        "/swagger"
    ];

    private static readonly PathString[] LimitedAllowedPrefixes =
    [
        "/api/users/me",
        "/api/tenant/status"
    ];

    private readonly RequestDelegate _next;

    public TenantAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, DapperContext db)
    {
        if (context.User.Identity?.IsAuthenticated != true || IsExcluded(context.Request.Path) || IsSuperAdmin(context))
        {
            await _next(context);
            return;
        }

        var tenantClaim = context.User.FindFirstValue("tenant_id");
        if (!Guid.TryParse(tenantClaim, out var tenantId))
        {
            await _next(context);
            return;
        }

        const string sql = @"
SELECT TOP (1)
    t.IsActive,
    COALESCE(t.SubscriptionState, CASE WHEN t.IsActive = 1 THEN N'Active' ELSE N'Disabled' END) AS State,
    t.TrialEndsAt,
    s.Status AS SubscriptionStatus,
    s.EndsAtUtc AS SubscriptionEndsAt,
    s.GracePeriodDays
FROM dbo.Tenants t
OUTER APPLY
(
    SELECT TOP (1) latest.Status, latest.EndsAtUtc, latest.GracePeriodDays
    FROM dbo.TenantSubscriptions latest
    WHERE latest.TenantId = t.Id
    ORDER BY latest.EndsAtUtc DESC, latest.CreatedAtUtc DESC
) s
WHERE t.Id = @TenantId;";

        using var connection = db.CreateConnection();
        var status = await connection.QueryFirstOrDefaultAsync<TenantAccessStatus>(sql, new { TenantId = tenantId });

        var now = DateTime.UtcNow;
        var denied =
            status is null ||
            !status.IsActive ||
            status.State.Equals("Suspended", StringComparison.OrdinalIgnoreCase) ||
            status.State.Equals("Expired", StringComparison.OrdinalIgnoreCase) ||
            status.SubscriptionStatus is 2 or 3 or 6 ||
            (status.SubscriptionEndsAt.HasValue &&
             status.SubscriptionEndsAt.Value < now &&
             now > status.SubscriptionEndsAt.Value.AddDays(status.GracePeriodDays));

        if (denied && !IsLimitedAllowed(context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Clinic subscription has expired. Please contact platform admin.",
                statusCode = StatusCodes.Status402PaymentRequired
            });
            return;
        }

        await _next(context);
    }

    private static bool IsExcluded(PathString path)
    {
        return ExcludedPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLimitedAllowed(PathString path)
    {
        return LimitedAllowedPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSuperAdmin(HttpContext context)
    {
        return context.User.IsInRole("SuperAdmin");
    }

    private sealed class TenantAccessStatus
    {
        public bool IsActive { get; set; }
        public string State { get; set; } = "Trial";
        public DateTime? TrialEndsAt { get; set; }
        public short? SubscriptionStatus { get; set; }
        public DateTime? SubscriptionEndsAt { get; set; }
        public int GracePeriodDays { get; set; }
    }
}
