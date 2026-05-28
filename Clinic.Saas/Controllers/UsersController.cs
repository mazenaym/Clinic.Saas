using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Users.Commands;
using Clinic.Saas.Service.UseCases.Users.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly CreateUserCommand.Handler _createUser;
    private readonly GetTenantUsersQuery.Handler _getTenantUsers;
    private readonly GetCurrentUserQuery.Handler _getCurrentUser;
    private readonly GetUserPreferencesQuery.Handler _getUserPreferences;
    private readonly SaveUserPreferencesCommand.Handler _saveUserPreferences;
    private readonly ICurrentUserService _currentUser;

    public UsersController(
        CreateUserCommand.Handler createUser,
        GetTenantUsersQuery.Handler getTenantUsers,
        GetCurrentUserQuery.Handler getCurrentUser,
        GetUserPreferencesQuery.Handler getUserPreferences,
        SaveUserPreferencesCommand.Handler saveUserPreferences,
        ICurrentUserService currentUser)
    {
        _createUser = createUser;
        _getTenantUsers = getTenantUsers;
        _getCurrentUser = getCurrentUser;
        _getUserPreferences = getUserPreferences;
        _saveUserPreferences = saveUserPreferences;
        _currentUser = currentUser;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _createUser.Handle(new CreateUserCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            User = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getTenantUsers.Handle(new GetTenantUsersQuery.Query
        {
            TenantId = _currentUser.TenantId.Value
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getCurrentUser.Handle(new GetCurrentUserQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = _currentUser.UserId.Value
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("me/preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getUserPreferences.Handle(new GetUserPreferencesQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = _currentUser.UserId.Value
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("me/preferences")]
    public async Task<IActionResult> SavePreferences([FromBody] UserPreferencesDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _saveUserPreferences.Handle(new SaveUserPreferencesCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = _currentUser.UserId.Value,
            Preferences = dto
        });

        return StatusCode(result.StatusCode, result);
    }
}
