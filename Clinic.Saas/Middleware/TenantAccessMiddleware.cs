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
        "/swagger"
    ];

    private readonly RequestDelegate _next;

    public TenantAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, DapperContext db)
    {
        if (context.User.Identity?.IsAuthenticated != true || IsExcluded(context.Request.Path))
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
SELECT TOP 1
    t.IsActive,
    COALESCE(t.SubscriptionState, N'Trial') AS State,
    t.TrialEndsAt,
    (
        SELECT TOP 1 s.EndDate
        FROM dbo.Subscriptions s
        WHERE s.TenantId = t.Id
          AND s.Status IN (1, 4)
        ORDER BY s.EndDate DESC
    ) AS SubscriptionEndsAt
FROM dbo.Tenants t
WHERE t.Id = @TenantId;";

        using var connection = db.CreateConnection();
        var status = await connection.QueryFirstOrDefaultAsync<TenantAccessStatus>(sql, new { TenantId = tenantId });

        var now = DateTime.UtcNow;
        var denied =
            status is null ||
            !status.IsActive ||
            status.State.Equals("Suspended", StringComparison.OrdinalIgnoreCase) ||
            status.State.Equals("Expired", StringComparison.OrdinalIgnoreCase) ||
            (status.State.Equals("Trial", StringComparison.OrdinalIgnoreCase) && status.TrialEndsAt.HasValue && status.TrialEndsAt.Value < now) ||
            (status.State.Equals("Active", StringComparison.OrdinalIgnoreCase) && status.SubscriptionEndsAt.HasValue && status.SubscriptionEndsAt.Value < now);

        if (denied)
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Clinic subscription is not active.",
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

    private sealed class TenantAccessStatus
    {
        public bool IsActive { get; set; }
        public string State { get; set; } = "Trial";
        public DateTime? TrialEndsAt { get; set; }
        public DateTime? SubscriptionEndsAt { get; set; }
    }
}
