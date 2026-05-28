using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Reports.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/reports")]
[ApiController]
[Authorize(Roles = "Admin,Reception")]
public class ReportsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly GetFinancialDuesReportQuery.Handler _financialDues;

    public ReportsController(
        ICurrentUserService currentUser,
        GetFinancialDuesReportQuery.Handler financialDues)
    {
        _currentUser = currentUser;
        _financialDues = financialDues;
    }

    [HttpGet("financial-dues")]
    public async Task<IActionResult> FinancialDues([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? doctorId)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _financialDues.Handle(new GetFinancialDuesReportQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            From = from,
            To = to,
            DoctorId = doctorId
        });

        return StatusCode(result.StatusCode, result);
    }
}
