using Clinic.Saas.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.ProblemDetails;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddClinicProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance = context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            };
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

                var problem = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Error",
                    Detail = "Validation failed for the request.",
                    Instance = context.HttpContext.Request.Path
                };

                problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                return new BadRequestObjectResult(problem);
            };
        });

        return services;
    }

    public static IApplicationBuilder UseClinicProblemDetails(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
                var (statusCode, title, detail) = MapException(exception);

                context.Response.StatusCode = statusCode;
                await context.RequestServices
                    .GetRequiredService<IProblemDetailsService>()
                    .WriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = context,
                        Exception = exception,
                        ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                        {
                            Status = statusCode,
                            Title = title,
                            Detail = detail
                        }
                    });
            });
        });

        app.UseStatusCodePages(async statusCodeContext =>
        {
            var httpContext = statusCodeContext.HttpContext;
            if (httpContext.Response.HasStarted || httpContext.Response.ContentLength.HasValue)
            {
                return;
            }

            var statusCode = httpContext.Response.StatusCode;
            if (statusCode < StatusCodes.Status400BadRequest)
            {
                return;
            }

            var (title, detail) = MapStatusCode(statusCode);
            await httpContext.RequestServices
                .GetRequiredService<IProblemDetailsService>()
                .WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Status = statusCode,
                        Title = title,
                        Detail = detail
                    }
                });
        });

        return app;
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception? exception) => exception switch
    {
        ConcurrencyConflictException ex => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
        RecordNotFoundException ex => (StatusCodes.Status404NotFound, "Not Found", ex.Message),
        UnauthorizedAccessException ex => (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
        InvalidOperationException ex => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
        _ => (StatusCodes.Status500InternalServerError, "Server Error", "An unexpected error occurred.")
    };

    private static (string Title, string Detail) MapStatusCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => ("Bad Request", "The request could not be processed."),
        StatusCodes.Status401Unauthorized => ("Unauthorized", "Authentication is required for this operation."),
        StatusCodes.Status403Forbidden => ("Forbidden", "You do not have permission to perform this operation."),
        StatusCodes.Status404NotFound => ("Not Found", "The requested resource was not found."),
        StatusCodes.Status409Conflict => ("Conflict", "The request conflicts with the current resource state."),
        _ when statusCode >= StatusCodes.Status500InternalServerError => ("Server Error", "An unexpected error occurred."),
        _ => ("Request Error", "The request failed.")
    };
}
