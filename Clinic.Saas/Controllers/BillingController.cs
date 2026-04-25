using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Payments.Commands;
using Clinic.Saas.Service.UseCases.Payments.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BillingController : ControllerBase
{
    private static readonly Guid DefaultTenantId = Guid.Parse("71CC36D9-A2E8-4441-90FB-118F2973375A");

    private readonly CreatePaymentCommand.Handler _createPayment;
    private readonly GetDailyRevenueReportQuery.Handler _dailyRevenue;

    public BillingController(CreatePaymentCommand.Handler createPayment, GetDailyRevenueReportQuery.Handler dailyRevenue)
    {
        _createPayment = createPayment;
        _dailyRevenue = dailyRevenue;
    }

    [HttpPost("payments")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        var result = await _createPayment.Handle(new CreatePaymentCommand.Command
        {
            TenantId = DefaultTenantId,
            Payment = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("reports/daily-revenue")]
    public async Task<IActionResult> DailyRevenue([FromQuery] DateTime date)
    {
        var result = await _dailyRevenue.Handle(new GetDailyRevenueReportQuery.Query
        {
            TenantId = DefaultTenantId,
            Date = date
        });

        return StatusCode(result.StatusCode, result);
    }
}
