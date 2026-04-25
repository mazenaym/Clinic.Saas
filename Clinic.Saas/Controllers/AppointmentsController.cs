using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Appointments.Commands;
using Clinic.Saas.Service.UseCases.Appointments.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly CreateAppointmentCommand.Handler _createAppointment;
    private readonly GetAppointmentsByDateQuery.Handler _getByDate;
    private readonly GetAppointmentAvailabilityQuery.Handler _availability;
    private readonly UpdateAppointmentStatusCommand.Handler _updateStatus;
    private readonly ICurrentUserService _currentUser;

    public AppointmentsController(
        CreateAppointmentCommand.Handler createAppointment,
        GetAppointmentsByDateQuery.Handler getByDate,
        GetAppointmentAvailabilityQuery.Handler availability,
        UpdateAppointmentStatusCommand.Handler updateStatus,
        ICurrentUserService currentUser)
    {
        _createAppointment = createAppointment;
        _getByDate = getByDate;
        _availability = availability;
        _updateStatus = updateStatus;
        _currentUser = currentUser;
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _createAppointment.Handle(new CreateAppointmentCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Appointment = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily([FromQuery] DateTime date)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getByDate.Handle(new GetAppointmentsByDateQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            Date = date
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability([FromQuery] Guid doctorId, [FromQuery] DateTime date)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _availability.Handle(new GetAppointmentAvailabilityQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            DoctorId = doctorId,
            Date = date
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAppointmentStatusDto dto)
    {
        dto.Id = id;
        var result = await _updateStatus.Handle(new UpdateAppointmentStatusCommand.Command { Request = dto });
        return StatusCode(result.StatusCode, result);
    }
}
