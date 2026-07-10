using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Auth.Commands;
using Clinic.Saas.api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly LoginCommand.Handler _login;
    private readonly RefreshTokenCommand.Handler _refresh;
    private readonly LogoutCommand.Handler _logout;
    private readonly ICurrentUserService _currentUser;
    private readonly ChangePasswordCommand.Handler _changePassword;

    public AuthController(
        LoginCommand.Handler login,
        RefreshTokenCommand.Handler refresh,
        LogoutCommand.Handler logout,
        ICurrentUserService currentUser , ChangePasswordCommand.Handler changePassword)
    {
        _login = login;
        _refresh = refresh;
        _logout = logout;
        _currentUser = currentUser;
        _changePassword = changePassword;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Subdomain) &&
            HttpContext.Items.TryGetValue(TenantResolutionMiddleware.TenantSubdomainItemKey, out var subdomain))
        {
            dto.Subdomain = subdomain?.ToString();
        }

        var result = await _login.Handle(new LoginCommand.Command { Request = dto });
        return StatusCode(result.StatusCode, result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var result = await _refresh.Handle(new RefreshTokenCommand.Command
        {
            Request = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _logout.Handle(new LogoutCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = _currentUser.UserId.Value
        });
        return StatusCode(result.StatusCode, result);
    }
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _changePassword.Handle(new ChangePasswordCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = _currentUser.UserId.Value,
            Request = dto
        });

        return StatusCode(result.StatusCode, result);
    }
}
