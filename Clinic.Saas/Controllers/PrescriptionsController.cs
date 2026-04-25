using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Prescriptions.Commands;
using Clinic.Saas.Service.UseCases.Prescriptions.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PrescriptionsController : ControllerBase
{
    private static readonly Guid DefaultTenantId = Guid.Parse("71CC36D9-A2E8-4441-90FB-118F2973375A");

    private readonly CreatePrescriptionCommand.Handler _createPrescription;
    private readonly GetPrescriptionByIdQuery.Handler _getPrescription;

    public PrescriptionsController(CreatePrescriptionCommand.Handler createPrescription, GetPrescriptionByIdQuery.Handler getPrescription)
    {
        _createPrescription = createPrescription;
        _getPrescription = getPrescription;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionDto dto)
    {
        var result = await _createPrescription.Handle(new CreatePrescriptionCommand.Command
        {
            TenantId = DefaultTenantId,
            Prescription = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _getPrescription.Handle(new GetPrescriptionByIdQuery.Query { Id = id });
        return StatusCode(result.StatusCode, result);
    }
}
