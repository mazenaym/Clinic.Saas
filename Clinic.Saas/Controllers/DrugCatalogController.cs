using Clinic.Saas.Service.UseCases.DrugCatalog.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/drug-catalog")]
[ApiController]
[Authorize]
public class DrugCatalogController : ControllerBase
{
    private readonly SearchDrugsQuery.Handler _searchDrugs;
    private readonly CheckDrugInteractionsQuery.Handler _checkInteractions;

    public DrugCatalogController(
        SearchDrugsQuery.Handler searchDrugs,
        CheckDrugInteractionsQuery.Handler checkInteractions)
    {
        _searchDrugs = searchDrugs;
        _checkInteractions = checkInteractions;
    }

    [HttpGet("drugs")]
    public async Task<IActionResult> Search([FromQuery] string term = "")
    {
        var result = await _searchDrugs.Handle(new SearchDrugsQuery.Query
        {
            Term = term
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("interactions")]
    public async Task<IActionResult> CheckInteractions([FromBody] string[] drugNames)
    {
        var result = await _checkInteractions.Handle(new CheckDrugInteractionsQuery.Query
        {
            DrugNames = drugNames
        });

        return StatusCode(result.StatusCode, result);
    }
}
