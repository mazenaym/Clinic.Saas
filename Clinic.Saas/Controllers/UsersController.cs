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
    private readonly ICurrentUserService _currentUser;

    public UsersController(
        CreateUserCommand.Handler createUser,
        GetTenantUsersQuery.Handler getTenantUsers,
        GetCurrentUserQuery.Handler getCurrentUser,
        ICurrentUserService currentUser)
    {
        _createUser = createUser;
        _getTenantUsers = getTenantUsers;
        _getCurrentUser = getCurrentUser;
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
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getCurrentUser.Handle(new GetCurrentUserQuery.Query
        {
            UserId = _currentUser.UserId.Value
        });

        return StatusCode(result.StatusCode, result);
    }
}
