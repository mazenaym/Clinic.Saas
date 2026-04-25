using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Visits.Commands;
using Clinic.Saas.Service.UseCases.Visits.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class VisitsController : ControllerBase
{
    private readonly CreateVisitCommand.Handler _createVisit;
    private readonly GetVisitByIdQuery.Handler _getVisit;
    private readonly ICurrentUserService _currentUser;

    public VisitsController(
        CreateVisitCommand.Handler createVisit,
        GetVisitByIdQuery.Handler getVisit,
        ICurrentUserService currentUser)
    {
        _createVisit = createVisit;
        _getVisit = getVisit;
        _currentUser = currentUser;
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVisitDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        if (_currentUser.Role == Domain.Enums.UserRole.Doctor && _currentUser.UserId.HasValue)
        {
            dto.DoctorId = _currentUser.UserId.Value;
        }

        var result = await _createVisit.Handle(new CreateVisitCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Visit = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _getVisit.Handle(new GetVisitByIdQuery.Query { Id = id });
        return StatusCode(result.StatusCode, result);
    }
}
