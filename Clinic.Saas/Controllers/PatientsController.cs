using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Patients.Commands;
using Clinic.Saas.Service.UseCases.Patients.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly CreatePatientCommand.Handler _createPatient;
    private readonly GetPatientByIdQuery.Handler _getPatient;
    private readonly GetAllPatientsQuery.Handler _getAllPatients;
    private readonly SearchPatientsQuery.Handler _searchPatients;
    private readonly UpdatePatientCommand.Handler _updatePatient;
    private readonly DeletePatientCommand.Handler _deletePatient;
    private readonly ICurrentUserService _currentUser;

    public PatientsController(
        CreatePatientCommand.Handler createPatient,
        GetPatientByIdQuery.Handler getPatient,
        GetAllPatientsQuery.Handler getAllPatients,
        SearchPatientsQuery.Handler searchPatients,
        UpdatePatientCommand.Handler updatePatient,
        DeletePatientCommand.Handler deletePatient,
        ICurrentUserService currentUser)
    {
        _createPatient = createPatient;
        _getPatient = getPatient;
        _getAllPatients = getAllPatients;
        _searchPatients = searchPatients;
        _updatePatient = updatePatient;
        _deletePatient = deletePatient;
        _currentUser = currentUser;
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        dto.TenantId = _currentUser.TenantId.Value.ToString();
        var result = await _createPatient.Handle(new CreatePatientCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Patient = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getPatient.Handle(new GetPatientByIdQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            Id = id
        });
        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _searchPatients.Handle(new SearchPatientsQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            SearchTerm = string.Empty
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _searchPatients.Handle(new SearchPatientsQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            SearchTerm = term ?? string.Empty
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        dto.Id = id;
        var result = await _updatePatient.Handle(new UpdatePatientCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Patient = dto
        });
        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Reception")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _deletePatient.Handle(new DeletePatientCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Id = id
        });
        return StatusCode(result.StatusCode, result);
    }
}
