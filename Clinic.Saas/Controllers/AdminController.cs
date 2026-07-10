using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Admin.Commands;
using Clinic.Saas.Service.UseCases.Admin.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Obsolete("Compatibility API. Use /api/platform endpoints.")]
public class AdminController : LegacyCompatibilityControllerBase
{
    private readonly GetAdminDashboardQuery.Handler _dashboard;
    private readonly BootstrapSuperAdminCommand.Handler _bootstrap;
    private readonly GetAdminClinicsQuery.Handler _clinics;
    private readonly GetAdminClinicByIdQuery.Handler _clinicById;
    private readonly CreateClinicCommand.Handler _createClinic;
    private readonly UpdateClinicCommand.Handler _updateClinic;
    private readonly SetClinicStatusCommand.Handler _setClinicStatus;
    private readonly CreateClinicSubscriptionCommand.Handler _createSubscription;

    public AdminController(
        GetAdminDashboardQuery.Handler dashboard,
        BootstrapSuperAdminCommand.Handler bootstrap,
        GetAdminClinicsQuery.Handler clinics,
        GetAdminClinicByIdQuery.Handler clinicById,
        CreateClinicCommand.Handler createClinic,
        UpdateClinicCommand.Handler updateClinic,
        SetClinicStatusCommand.Handler setClinicStatus,
        CreateClinicSubscriptionCommand.Handler createSubscription)
    {
        _dashboard = dashboard;
        _bootstrap = bootstrap;
        _clinics = clinics;
        _clinicById = clinicById;
        _createClinic = createClinic;
        _updateClinic = updateClinic;
        _setClinicStatus = setClinicStatus;
        _createSubscription = createSubscription;
    }

    [AllowAnonymous]
    [HttpPost("bootstrap")]
    public async Task<IActionResult> Bootstrap([FromBody] BootstrapSuperAdminDto dto)
    {
        var result = await _bootstrap.Handle(new BootstrapSuperAdminCommand.Command
        {
            Request = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var result = await _dashboard.Handle();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("clinics")]
    public async Task<IActionResult> GetClinics()
    {
        var result = await _clinics.Handle();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("clinics/{clinicId:guid}")]
    public async Task<IActionResult> GetClinic(Guid clinicId)
    {
        var result = await _clinicById.Handle(new GetAdminClinicByIdQuery.Query
        {
            ClinicId = clinicId
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("clinics")]
    public async Task<IActionResult> CreateClinic([FromBody] CreateClinicDto dto)
    {
        var result = await _createClinic.Handle(new CreateClinicCommand.Command
        {
            Clinic = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("clinics/{clinicId:guid}")]
    public async Task<IActionResult> UpdateClinic(Guid clinicId, [FromBody] UpdateClinicDto dto)
    {
        var result = await _updateClinic.Handle(new UpdateClinicCommand.Command
        {
            ClinicId = clinicId,
            Clinic = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("clinics/{clinicId:guid}/status")]
    public async Task<IActionResult> SetClinicStatus(Guid clinicId, [FromBody] SetClinicStatusDto dto)
    {
        var result = await _setClinicStatus.Handle(new SetClinicStatusCommand.Command
        {
            ClinicId = clinicId,
            IsActive = dto.IsActive
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("clinics/{clinicId:guid}/subscriptions")]
    public async Task<IActionResult> AddSubscription(Guid clinicId, [FromBody] CreateSubscriptionDto dto)
    {
        var result = await _createSubscription.Handle(new CreateClinicSubscriptionCommand.Command
        {
            ClinicId = clinicId,
            Subscription = dto
        });

        return StatusCode(result.StatusCode, result);
    }
}

public class SetClinicStatusDto
{
    public bool IsActive { get; set; }
}
