using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Onboarding.Commands;
using Clinic.Saas.Service.UseCases.Onboarding.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/onboarding")]
[ApiController]
[AllowAnonymous]
public class OnboardingController : ControllerBase
{
    private readonly RegisterClinicCommand.Handler _registerClinic;
    private readonly CheckSubdomainAvailabilityQuery.Handler _checkSubdomain;

    public OnboardingController(
        RegisterClinicCommand.Handler registerClinic,
        CheckSubdomainAvailabilityQuery.Handler checkSubdomain)
    {
        _registerClinic = registerClinic;
        _checkSubdomain = checkSubdomain;
    }

    [HttpGet("check-subdomain")]
    public async Task<IActionResult> CheckSubdomain([FromQuery] string subdomain)
    {
        var result = await _checkSubdomain.Handle(new CheckSubdomainAvailabilityQuery.Query
        {
            Subdomain = subdomain ?? string.Empty
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("register-clinic")]
    public async Task<IActionResult> RegisterClinic([FromBody] RegisterClinicDto dto)
    {
        var result = await _registerClinic.Handle(new RegisterClinicCommand.Command
        {
            Request = dto
        });

        return StatusCode(result.StatusCode, result);
    }
}
