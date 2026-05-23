using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Prescriptions.Commands;
using Clinic.Saas.Service.UseCases.Prescriptions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly CreatePrescriptionCommand.Handler _createPrescription;
    private readonly GetPrescriptionByIdQuery.Handler _getPrescription;
    private readonly ICurrentUserService _currentUser;
    private readonly IClinicAuthorizationService _authorization;

    public PrescriptionsController(
        CreatePrescriptionCommand.Handler createPrescription,
        GetPrescriptionByIdQuery.Handler getPrescription,
        ICurrentUserService currentUser,
        IClinicAuthorizationService authorization)
    {
        _createPrescription = createPrescription;
        _getPrescription = getPrescription;
        _currentUser = currentUser;
        _authorization = authorization;
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        if (_currentUser.Role == Domain.Enums.UserRole.Doctor && _currentUser.UserId.HasValue)
        {
            dto.DoctorId = _currentUser.UserId.Value;
        }

        var result = await _createPrescription.Handle(new CreatePrescriptionCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Prescription = dto
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

        if (!await _authorization.CanViewPrescriptionAsync(_currentUser.TenantId.Value, id))
        {
            return Forbid();
        }

        var result = await _getPrescription.Handle(new GetPrescriptionByIdQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            Id = id
        });
        return StatusCode(result.StatusCode, result);
    }
}
