using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Visits.Commands;
using Clinic.Saas.Service.UseCases.Visits.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/visits")]
[ApiController]
[Authorize]
public class VisitsController : ControllerBase
{
    private readonly CreateVisitCommand.Handler _createVisit;
    private readonly GetVisitByIdQuery.Handler _getVisit;
    private readonly GetPatientVisitsQuery.Handler _getPatientVisits;
    private readonly UpdateVisitCommand.Handler _updateVisit;
    private readonly FinalizeVisitCommand.Handler _finalizeVisit;
    private readonly ICurrentUserService _currentUser;
    private readonly IClinicAuthorizationService _authorization;
    private readonly IAuditService _auditService;

    public VisitsController(
        CreateVisitCommand.Handler createVisit,
        GetVisitByIdQuery.Handler getVisit,
        GetPatientVisitsQuery.Handler getPatientVisits,
        UpdateVisitCommand.Handler updateVisit,
        FinalizeVisitCommand.Handler finalizeVisit,
        ICurrentUserService currentUser,
        IClinicAuthorizationService authorization,
        IAuditService auditService)
    {
        _createVisit = createVisit;
        _getVisit = getVisit;
        _getPatientVisits = getPatientVisits;
        _updateVisit = updateVisit;
        _finalizeVisit = finalizeVisit;
        _currentUser = currentUser;
        _authorization = authorization;
        _auditService = auditService;
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

    [Authorize(Roles = "Admin,Doctor")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        if (!await _authorization.CanViewVisitAsync(_currentUser.TenantId.Value, id))
        {
            return Forbid();
        }

        var result = await _getVisit.Handle(new GetVisitByIdQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            Id = id
        });
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetByPatient(Guid patientId)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getPatientVisits.Handle(new GetPatientVisitsQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            PatientId = patientId
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVisitDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _updateVisit.Handle(new UpdateVisitCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            VisitId = id,
            Visit = dto
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Update", "Visit", id, new { id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPost("{id:guid}/finalize")]
    public async Task<IActionResult> Finalize(Guid id, [FromHeader(Name = "If-Match")] string? rowVersion)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _finalizeVisit.Handle(new FinalizeVisitCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            VisitId = id,
            FinalizedByUserId = _currentUser.UserId.Value,
            RowVersion = rowVersion
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Finalize", "Visit", id, new { id });
        }

        return StatusCode(result.StatusCode, result);
    }
}
