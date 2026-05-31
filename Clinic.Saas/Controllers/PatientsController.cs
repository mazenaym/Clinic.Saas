using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.Security;
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
    private readonly GetPatientTimelineQuery.Handler _getTimeline;
    private readonly GetPatientChartQuery.Handler _getChart;
    private readonly GetPatientFinancialLedgerQuery.Handler _getLedger;
    private readonly FindPatientDuplicatesQuery.Handler _findDuplicates;
    private readonly ExportPatientsQuery.Handler _exportPatients;
    private readonly UpdatePatientCommand.Handler _updatePatient;
    private readonly DeletePatientCommand.Handler _deletePatient;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public PatientsController(
        CreatePatientCommand.Handler createPatient,
        GetPatientByIdQuery.Handler getPatient,
        GetAllPatientsQuery.Handler getAllPatients,
        SearchPatientsQuery.Handler searchPatients,
        GetPatientTimelineQuery.Handler getTimeline,
        GetPatientChartQuery.Handler getChart,
        GetPatientFinancialLedgerQuery.Handler getLedger,
        FindPatientDuplicatesQuery.Handler findDuplicates,
        ExportPatientsQuery.Handler exportPatients,
        UpdatePatientCommand.Handler updatePatient,
        DeletePatientCommand.Handler deletePatient,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _createPatient = createPatient;
        _getPatient = getPatient;
        _getAllPatients = getAllPatients;
        _searchPatients = searchPatients;
        _getTimeline = getTimeline;
        _getChart = getChart;
        _getLedger = getLedger;
        _findDuplicates = findDuplicates;
        _exportPatients = exportPatients;
        _updatePatient = updatePatient;
        _deletePatient = deletePatient;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [Authorize(Roles = "Admin,Doctor,Reception", Policy = Permissions.PatientsEdit)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

       
        var result = await _createPatient.Handle(new CreatePatientCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Patient = dto
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Create", "Patient", result.Data?.Id, new { result.Data?.Id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception", Policy = Permissions.PatientsView)]
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

    [Authorize(Roles = "Admin,Doctor,Reception", Policy = Permissions.PatientsView)]
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

    [Authorize(Roles = "Admin,Doctor,Reception", Policy = Permissions.PatientsView)]
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

    [Authorize(Roles = "Admin,Doctor,Reception", Policy = Permissions.PatientsClinicalView)]
    [HttpGet("{id:guid}/chart")]
    public async Task<IActionResult> Chart(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getChart.Handle(new GetPatientChartQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            PatientId = id
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception", Policy = Permissions.BillingView)]
    [HttpGet("{id:guid}/ledger")]
    public async Task<IActionResult> Ledger(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getLedger.Handle(new GetPatientFinancialLedgerQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            PatientId = id
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception", Policy = Permissions.PatientsClinicalView)]
    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> Timeline(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getTimeline.Handle(new GetPatientTimelineQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            PatientId = id
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception", Policy = Permissions.PatientsView)]
    [HttpGet("duplicates")]
    public async Task<IActionResult> Duplicates([FromQuery] string? phone, [FromQuery] string? nationalId)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _findDuplicates.Handle(new FindPatientDuplicatesQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            Phone = phone,
            NationalId = nationalId
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Reception", Policy = Permissions.PatientsView)]
    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _exportPatients.Handle(new ExportPatientsQuery.Query
        {
            TenantId = _currentUser.TenantId.Value
        });

        if (!result.Success || result.Data is null)
        {
            return StatusCode(result.StatusCode, result);
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [Authorize(Roles = "Admin,Doctor,Reception", Policy = Permissions.PatientsEdit)]
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
        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Update", "Patient", id, new { id });
        }
        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Reception", Policy = Permissions.PatientsEdit)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, [FromHeader(Name = "If-Match")] string? rowVersion)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _deletePatient.Handle(new DeletePatientCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Id = id,
            RowVersion = rowVersion
        });
        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Delete", "Patient", id, new { id });
        }
        return StatusCode(result.StatusCode, result);
    }
}
