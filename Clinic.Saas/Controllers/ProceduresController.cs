using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Procedures.Commands;
using Clinic.Saas.Service.UseCases.Procedures.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/procedures")]
[ApiController]
[Authorize(Roles = "Admin,Doctor,Reception")]
public class ProceduresController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly ListProceduresQuery.Handler _listProcedures;
    private readonly CreateProcedureCommand.Handler _createProcedure;
    private readonly UpdateProcedureCommand.Handler _updateProcedure;
    private readonly SetProcedureActiveCommand.Handler _setProcedureActive;

    public ProceduresController(
        ICurrentUserService currentUser,
        ListProceduresQuery.Handler listProcedures,
        CreateProcedureCommand.Handler createProcedure,
        UpdateProcedureCommand.Handler updateProcedure,
        SetProcedureActiveCommand.Handler setProcedureActive)
    {
        _currentUser = currentUser;
        _listProcedures = listProcedures;
        _createProcedure = createProcedure;
        _updateProcedure = updateProcedure;
        _setProcedureActive = setProcedureActive;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _listProcedures.Handle(new ListProceduresQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            IncludeInactive = includeInactive
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProcedureDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _createProcedure.Handle(new CreateProcedureCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Procedure = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProcedureDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _updateProcedure.Handle(new UpdateProcedureCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            ProcedureId = id,
            Procedure = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        return await SetActive(id, true);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        return await SetActive(id, false);
    }

    private async Task<IActionResult> SetActive(Guid id, bool isActive)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _setProcedureActive.Handle(new SetProcedureActiveCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            ProcedureId = id,
            IsActive = isActive
        });

        return StatusCode(result.StatusCode, result);
    }
}
