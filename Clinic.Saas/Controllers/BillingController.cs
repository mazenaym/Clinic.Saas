using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Payments.Commands;
using Clinic.Saas.Service.UseCases.Payments.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly CreatePaymentCommand.Handler _createPayment;
    private readonly GetDailyRevenueReportQuery.Handler _dailyRevenue;
    private readonly ICurrentUserService _currentUser;

    public BillingController(
        CreatePaymentCommand.Handler createPayment,
        GetDailyRevenueReportQuery.Handler dailyRevenue,
        ICurrentUserService currentUser)
    {
        _createPayment = createPayment;
        _dailyRevenue = dailyRevenue;
        _currentUser = currentUser;
    }

    [Authorize(Roles = "Admin,Reception")]
    [HttpPost("payments")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _createPayment.Handle(new CreatePaymentCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Payment = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("reports/daily-revenue")]
    public async Task<IActionResult> DailyRevenue([FromQuery] DateTime date)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _dailyRevenue.Handle(new GetDailyRevenueReportQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            Date = date
        });

        return StatusCode(result.StatusCode, result);
    }
}
