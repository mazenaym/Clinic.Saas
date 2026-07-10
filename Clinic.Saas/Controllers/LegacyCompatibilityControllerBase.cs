using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Clinic.Saas.api.Controllers;

/// <summary>Common HTTP-only behavior for deprecated compatibility endpoints.</summary>
public abstract class LegacyCompatibilityControllerBase : ControllerBase, IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        Response.Headers["Deprecation"] = "true";
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
