using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.OnlineBookings.Commands;
using Clinic.Saas.Service.UseCases.OnlineBookings.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/online-bookings")]
[ApiController]
[Authorize]
public class OnlineBookingsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly GetOnlineBookingsQuery.Handler _getOnlineBookings;
    private readonly ApproveOnlineBookingCommand.Handler _approveOnlineBooking;
    private readonly RejectOnlineBookingCommand.Handler _rejectOnlineBooking;

    public OnlineBookingsController(
        ICurrentUserService currentUser,
        GetOnlineBookingsQuery.Handler getOnlineBookings,
        ApproveOnlineBookingCommand.Handler approveOnlineBooking,
        RejectOnlineBookingCommand.Handler rejectOnlineBooking)
    {
        _currentUser = currentUser;
        _getOnlineBookings = getOnlineBookings;
        _approveOnlineBooking = approveOnlineBooking;
        _rejectOnlineBooking = rejectOnlineBooking;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getOnlineBookings.Handle(new GetOnlineBookingsQuery.Query
        {
            TenantId = _currentUser.TenantId.Value
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _approveOnlineBooking.Handle(new ApproveOnlineBookingCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            BookingId = id
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectOnlineBookingDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _rejectOnlineBooking.Handle(new RejectOnlineBookingCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            BookingId = id,
            Rejection = dto
        });

        return StatusCode(result.StatusCode, result);
    }
}
