using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.ClinicalTemplates.Commands;
using Clinic.Saas.Service.UseCases.ClinicalTemplates.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/clinical-templates")]
[ApiController]
[Authorize]
public class ClinicalTemplatesController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly GetClinicalTemplatesQuery.Handler _getClinicalTemplates;
    private readonly CreateClinicalTemplateCommand.Handler _createClinicalTemplate;

    public ClinicalTemplatesController(
        ICurrentUserService currentUser,
        GetClinicalTemplatesQuery.Handler getClinicalTemplates,
        CreateClinicalTemplateCommand.Handler createClinicalTemplate)
    {
        _currentUser = currentUser;
        _getClinicalTemplates = getClinicalTemplates;
        _createClinicalTemplate = createClinicalTemplate;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getClinicalTemplates.Handle(new GetClinicalTemplatesQuery.Query
        {
            TenantId = _currentUser.TenantId.Value
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClinicalTemplateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _createClinicalTemplate.Handle(new CreateClinicalTemplateCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Template = dto
        });

        return StatusCode(result.StatusCode, result);
    }
}
