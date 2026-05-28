using Clinic.Saas.Service.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Clinic.Saas.api.ProblemDetails;

public class BaseResponseProblemDetailsFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult objectResult &&
            TryCreateFromBaseResponse(context.HttpContext, objectResult.Value, objectResult.StatusCode, out var problemResult))
        {
            context.Result = problemResult;
            return;
        }

        if (context.Result is StatusCodeResult statusCodeResult && statusCodeResult.StatusCode >= StatusCodes.Status400BadRequest)
        {
            context.Result = CreateProblemResult(
                context.HttpContext,
                statusCodeResult.StatusCode,
                DefaultTitle(statusCodeResult.StatusCode),
                DefaultDetail(statusCodeResult.StatusCode));
            return;
        }

        if (context.Result is ForbidResult)
        {
            context.Result = CreateProblemResult(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You do not have permission to perform this operation.");
            return;
        }

        if (context.Result is UnauthorizedResult)
        {
            context.Result = CreateProblemResult(
                context.HttpContext,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required for this operation.");
        }
    }

    private static bool TryCreateFromBaseResponse(HttpContext httpContext, object? value, int? resultStatusCode, out ObjectResult problemResult)
    {
        problemResult = null!;

        if (value is null)
        {
            return false;
        }

        var type = value.GetType();
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(BaseResponse<>))
        {
            return false;
        }

        var success = (bool?)type.GetProperty(nameof(BaseResponse<object>.Success))?.GetValue(value) ?? true;
        if (success)
        {
            return false;
        }

        var statusCode = (int?)type.GetProperty(nameof(BaseResponse<object>.StatusCode))?.GetValue(value)
            ?? resultStatusCode
            ?? StatusCodes.Status400BadRequest;
        var message = (string?)type.GetProperty(nameof(BaseResponse<object>.Message))?.GetValue(value);
        var errors = type.GetProperty(nameof(BaseResponse<object>.Errors))?.GetValue(value) as IEnumerable<string>;

        problemResult = CreateProblemResult(
            httpContext,
            statusCode,
            DefaultTitle(statusCode),
            string.IsNullOrWhiteSpace(message) ? DefaultDetail(statusCode) : message,
            errors);
        return true;
    }

    private static ObjectResult CreateProblemResult(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        IEnumerable<string>? errors = null)
    {
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        var errorList = errors?.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray();
        if (errorList is { Length: > 0 })
        {
            problem.Extensions["errors"] = errorList;
        }

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            DeclaredType = typeof(Microsoft.AspNetCore.Mvc.ProblemDetails)
        };
    }

    private static string DefaultTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Validation Error",
        _ when statusCode >= StatusCodes.Status500InternalServerError => "Server Error",
        _ => "Request Error"
    };

    private static string DefaultDetail(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "The request could not be processed.",
        StatusCodes.Status401Unauthorized => "Authentication is required for this operation.",
        StatusCodes.Status403Forbidden => "You do not have permission to perform this operation.",
        StatusCodes.Status404NotFound => "The requested resource was not found.",
        StatusCodes.Status409Conflict => "The request conflicts with the current resource state.",
        StatusCodes.Status422UnprocessableEntity => "Validation failed for the request.",
        _ when statusCode >= StatusCodes.Status500InternalServerError => "An unexpected error occurred.",
        _ => "The request failed."
    };
}
