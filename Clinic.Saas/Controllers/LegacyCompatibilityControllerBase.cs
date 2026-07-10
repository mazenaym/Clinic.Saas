using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Clinic.Saas.api.Controllers;

/// <summary>Common HTTP-only behavior for deprecated compatibility endpoints.</summary>
public abstract class LegacyCompatibilityControllerBase : ControllerBase, IActionFilter
{
    protected void AddSuccessor(string route) => Response.Headers["Link"] = $"<{route}>; rel=\"successor-version\"";
    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        Response.Headers["Deprecation"] = "true";
        var path = Request.Path.Value;
        if (path?.StartsWith("/api/admin/plans", StringComparison.OrdinalIgnoreCase) == true)
            AddSuccessor(path.Replace("/api/admin/plans", "/api/platform/plans", StringComparison.OrdinalIgnoreCase));
    }

    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context) { }
}
