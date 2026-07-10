using Clinic.Saas.Service.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

public abstract class PlatformControllerBase : ControllerBase
{
    protected static IActionResult OkResponse<T>(T data, string message = "OK") => new OkObjectResult(Success(data, message, 200));
    protected static IActionResult NotFoundResponse<T>(string message) => new NotFoundObjectResult(Success<T?>(default, message, 404));
    protected static BaseResponse<T> Success<T>(T data, string message, int statusCode) => new() { Success = statusCode < 400, Message = message, Data = data, StatusCode = statusCode };
}
