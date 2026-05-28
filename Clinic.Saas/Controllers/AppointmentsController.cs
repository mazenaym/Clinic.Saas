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
    private readonly GetAppointmentRangeQuery.Handler _getRange;
    private readonly GetAppointmentCancellationsQuery.Handler _getCancellations;
    private readonly GetAppointmentAvailabilityQuery.Handler _availability;
    private readonly RescheduleAppointmentCommand.Handler _rescheduleAppointment;
    private readonly UpdateAppointmentStatusCommand.Handler _updateStatus;
    private readonly ICurrentUserService _currentUser;
    private readonly IClinicAuthorizationService _authorization;
    private readonly IAuditService _auditService;

    public AppointmentsController(
        CreateAppointmentCommand.Handler createAppointment,
        GetAppointmentsByDateQuery.Handler getByDate,
        GetAppointmentRangeQuery.Handler getRange,
        GetAppointmentCancellationsQuery.Handler getCancellations,
        GetAppointmentAvailabilityQuery.Handler availability,
        RescheduleAppointmentCommand.Handler rescheduleAppointment,
        UpdateAppointmentStatusCommand.Handler updateStatus,
        ICurrentUserService currentUser,
        IClinicAuthorizationService authorization,
        IAuditService auditService)
    {
        _createAppointment = createAppointment;
        _getByDate = getByDate;
        _getRange = getRange;
        _getCancellations = getCancellations;
        _availability = availability;
        _rescheduleAppointment = rescheduleAppointment;
        _updateStatus = updateStatus;
        _currentUser = currentUser;
        _authorization = authorization;
        _auditService = auditService;
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        if (!await _authorization.CanAccessDoctorScheduleAsync(dto.DoctorId))
        {
            return Forbid();
        }

        var result = await _createAppointment.Handle(new CreateAppointmentCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Appointment = dto
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Create", "Appointment", result.Data?.Id, new { result.Data?.Id });
        }

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
            Date = date,
            DoctorId = _currentUser.Role == Domain.Enums.UserRole.Doctor ? _currentUser.UserId : null
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpGet("weekly")]
    public Task<IActionResult> GetWeekly([FromQuery] DateTime weekStart)
    {
        return GetRange(weekStart.Date, weekStart.Date.AddDays(7));
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpGet("monthly")]
    public Task<IActionResult> GetMonthly([FromQuery] int year, [FromQuery] int month)
    {
        var start = new DateTime(year, month, 1);
        return GetRange(start, start.AddMonths(1));
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability([FromQuery] Guid doctorId, [FromQuery] DateTime date)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        if (!await _authorization.CanAccessDoctorScheduleAsync(doctorId))
        {
            return Forbid();
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
    [HttpPut("{id:guid}/reschedule")]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] RescheduleAppointmentDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        if (!await _authorization.CanUpdateAppointmentAsync(_currentUser.TenantId.Value, id))
        {
            return Forbid();
        }

        var result = await _rescheduleAppointment.Handle(new RescheduleAppointmentCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            AppointmentId = id,
            Request = dto
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Reschedule", "Appointment", id, new { id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpGet("cancellations")]
    public async Task<IActionResult> GetCancellations([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getCancellations.Handle(new GetAppointmentCancellationsQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            From = from.Date,
            To = to.Date
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor,Reception")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAppointmentStatusDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        dto.Id = id;
        if (!await _authorization.CanUpdateAppointmentAsync(_currentUser.TenantId.Value, id))
        {
            return Forbid();
        }

        var result = await _updateStatus.Handle(new UpdateAppointmentStatusCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Request = dto
        });
        if (result.Success)
        {
            var action = dto.Status == Domain.Enums.AppointmentStatus.Cancelled ? "Cancel" : "UpdateStatus";
            await this.AuditAsync(_auditService, _currentUser, action, "Appointment", id, new { id, status = dto.Status.ToString() });
        }
        return StatusCode(result.StatusCode, result);
    }

    private async Task<IActionResult> GetRange(DateTime from, DateTime to)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getRange.Handle(new GetAppointmentRangeQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            From = from.Date,
            To = to.Date,
            DoctorId = _currentUser.Role == Domain.Enums.UserRole.Doctor ? _currentUser.UserId : null
        });

        return StatusCode(result.StatusCode, result);
    }
}
