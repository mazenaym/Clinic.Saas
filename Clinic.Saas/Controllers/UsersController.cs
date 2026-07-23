using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.Security;
using Clinic.Saas.Service.UseCases.Users.Commands;
using Clinic.Saas.Service.UseCases.Users.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/users")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly CreateUserCommand.Handler _createUser;
    private readonly UpdateUserCommand.Handler _updateUser;
    private readonly DeactivateUserCommand.Handler _deactivateUser;
    private readonly ResetUserPasswordCommand.Handler _resetUserPassword;
    private readonly GetTenantUsersQuery.Handler _getTenantUsers;
    private readonly GetCurrentUserQuery.Handler _getCurrentUser;
    private readonly GetUserPreferencesQuery.Handler _getUserPreferences;
    private readonly SaveUserPreferencesCommand.Handler _saveUserPreferences;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public UsersController(
        CreateUserCommand.Handler createUser,
        UpdateUserCommand.Handler updateUser,
        DeactivateUserCommand.Handler deactivateUser,
        ResetUserPasswordCommand.Handler resetUserPassword,
        GetTenantUsersQuery.Handler getTenantUsers,
        GetCurrentUserQuery.Handler getCurrentUser,
        GetUserPreferencesQuery.Handler getUserPreferences,
        SaveUserPreferencesCommand.Handler saveUserPreferences,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _createUser = createUser;
        _updateUser = updateUser;
        _deactivateUser = deactivateUser;
        _resetUserPassword = resetUserPassword;
        _getTenantUsers = getTenantUsers;
        _getCurrentUser = getCurrentUser;
        _getUserPreferences = getUserPreferences;
        _saveUserPreferences = saveUserPreferences;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [Authorize(Roles = "Admin", Policy = Permissions.UsersManage)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _updateUser.Handle(new UpdateUserCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = id,
            User = dto
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Update", "User", id, new { id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin", Policy = Permissions.UsersManage)]
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _deactivateUser.Handle(new DeactivateUserCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = id
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Deactivate", "User", id, new { id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin", Policy = Permissions.UsersManage)]
    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _resetUserPassword.Handle(new ResetUserPasswordCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = id,
            Password = dto,
            CallerRole = _currentUser.Role!.Value
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "ResetPassword", "User", id, new { id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin", Policy = Permissions.UsersManage)]
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

    [Authorize(Roles = "Admin", Policy = Permissions.UsersManage)]
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

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "UpdatePreferences", "User", _currentUser.UserId.Value, new { id = _currentUser.UserId.Value });
        }

        return StatusCode(result.StatusCode, result);
    }
}
