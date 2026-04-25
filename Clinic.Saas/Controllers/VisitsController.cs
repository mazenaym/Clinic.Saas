using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Visits.Commands;
using Clinic.Saas.Service.UseCases.Visits.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VisitsController : ControllerBase
{
    private static readonly Guid DefaultTenantId = Guid.Parse("71CC36D9-A2E8-4441-90FB-118F2973375A");

    private readonly CreateVisitCommand.Handler _createVisit;
    private readonly GetVisitByIdQuery.Handler _getVisit;

    public VisitsController(CreateVisitCommand.Handler createVisit, GetVisitByIdQuery.Handler getVisit)
    {
        _createVisit = createVisit;
        _getVisit = getVisit;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVisitDto dto)
    {
        var result = await _createVisit.Handle(new CreateVisitCommand.Command
        {
            TenantId = DefaultTenantId,
            Visit = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _getVisit.Handle(new GetVisitByIdQuery.Query { Id = id });
        return StatusCode(result.StatusCode, result);
    }
}
