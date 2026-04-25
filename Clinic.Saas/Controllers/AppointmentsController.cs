using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Appointments.Commands;
using Clinic.Saas.Service.UseCases.Appointments.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AppointmentsController : ControllerBase
{
    private static readonly Guid DefaultTenantId = Guid.Parse("71CC36D9-A2E8-4441-90FB-118F2973375A");

    private readonly CreateAppointmentCommand.Handler _createAppointment;
    private readonly GetAppointmentsByDateQuery.Handler _getByDate;
    private readonly GetAppointmentAvailabilityQuery.Handler _availability;
    private readonly UpdateAppointmentStatusCommand.Handler _updateStatus;

    public AppointmentsController(
        CreateAppointmentCommand.Handler createAppointment,
        GetAppointmentsByDateQuery.Handler getByDate,
        GetAppointmentAvailabilityQuery.Handler availability,
        UpdateAppointmentStatusCommand.Handler updateStatus)
    {
        _createAppointment = createAppointment;
        _getByDate = getByDate;
        _availability = availability;
        _updateStatus = updateStatus;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        var result = await _createAppointment.Handle(new CreateAppointmentCommand.Command
        {
            TenantId = DefaultTenantId,
            Appointment = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily([FromQuery] DateTime date)
    {
        var result = await _getByDate.Handle(new GetAppointmentsByDateQuery.Query
        {
            TenantId = DefaultTenantId,
            Date = date
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability([FromQuery] Guid doctorId, [FromQuery] DateTime date)
    {
        var result = await _availability.Handle(new GetAppointmentAvailabilityQuery.Query
        {
            TenantId = DefaultTenantId,
            DoctorId = doctorId,
            Date = date
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAppointmentStatusDto dto)
    {
        dto.Id = id;
        var result = await _updateStatus.Handle(new UpdateAppointmentStatusCommand.Command { Request = dto });
        return StatusCode(result.StatusCode, result);
    }
}
