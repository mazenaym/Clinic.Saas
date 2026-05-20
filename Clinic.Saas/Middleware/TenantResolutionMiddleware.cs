using Clinic.Saas.Domain.Interfaces;

namespace Clinic.Saas.api.Middleware;

public class TenantResolutionMiddleware
{
    public const string TenantIdItemKey = "TenantId";
    public const string TenantSubdomainItemKey = "TenantSubdomain";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantRepository tenantRepository)
    {
        var subdomain = ResolveSubdomain(context.Request.Host.Host);
        if (!string.IsNullOrWhiteSpace(subdomain))
        {
            var tenant = await tenantRepository.GetBySubdomainAsync(subdomain);
            if (tenant is not null)
            {
                context.Items[TenantIdItemKey] = tenant.Id;
                context.Items[TenantSubdomainItemKey] = tenant.Subdomain;
            }
        }

        await _next(context);
    }

    private static string? ResolveSubdomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host) ||
            host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("::1", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 3 ? parts[0] : null;
    }
}
