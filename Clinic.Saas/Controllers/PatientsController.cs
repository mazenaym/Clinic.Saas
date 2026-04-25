using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Patients.Commands;
using Clinic.Saas.Service.UseCases.Patients.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PatientsController : ControllerBase
{
    private static readonly Guid DefaultTenantId = Guid.Parse("71CC36D9-A2E8-4441-90FB-118F2973375A");

    private readonly CreatePatientCommand.Handler _createPatient;
    private readonly GetPatientByIdQuery.Handler _getPatient;
    private readonly GetAllPatientsQuery.Handler _getAllPatients;
    private readonly SearchPatientsQuery.Handler _searchPatients;
    private readonly UpdatePatientCommand.Handler _updatePatient;
    private readonly DeletePatientCommand.Handler _deletePatient;

    public PatientsController(
        CreatePatientCommand.Handler createPatient,
        GetPatientByIdQuery.Handler getPatient,
        GetAllPatientsQuery.Handler getAllPatients,
        SearchPatientsQuery.Handler searchPatients,
        UpdatePatientCommand.Handler updatePatient,
        DeletePatientCommand.Handler deletePatient)
    {
        _createPatient = createPatient;
        _getPatient = getPatient;
        _getAllPatients = getAllPatients;
        _searchPatients = searchPatients;
        _updatePatient = updatePatient;
        _deletePatient = deletePatient;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
    {
        var tenantId = Guid.TryParse(dto.TenantId, out var parsedTenantId)
            ? parsedTenantId
            : DefaultTenantId;

        var command = new CreatePatientCommand.Command
        {
            TenantId = tenantId,
            Patient = dto
        };

        var result = await _createPatient.Handle(command);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _getPatient.Handle(new GetPatientByIdQuery.Query { Id = id });
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _getAllPatients.Handle();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        var result = await _searchPatients.Handle(new SearchPatientsQuery.Query
        {
            TenantId = DefaultTenantId,
            SearchTerm = term ?? string.Empty
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientDto dto)
    {
        dto.Id = id;
        var result = await _updatePatient.Handle(new UpdatePatientCommand.Command { Patient = dto });
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _deletePatient.Handle(new DeletePatientCommand.Command { Id = id });
        return StatusCode(result.StatusCode, result);
    }
}
