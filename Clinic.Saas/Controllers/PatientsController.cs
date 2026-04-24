using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Patients.Commands;
using Clinic.Saas.Service.UseCases.Patients.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly CreatePatientCommand.Handler _createPatient;
        private readonly GetPatientByIdQuery.Handler _getPatient;

        public PatientsController(CreatePatientCommand.Handler createPatient,GetPatientByIdQuery.Handler getPatient)
        {
            _createPatient = createPatient;
            _getPatient = getPatient;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
        {
            // TODO: Get TenantId from JWT later
            //var tenantId = Guid.Parse("fb-118f2973375a");

            var command = new CreatePatientCommand.Command
            {
                //TenantId = tenantId,
                Patient = dto
            };

            var result = await _createPatient.Handle(command);

            return result.Success
                ? StatusCode(result.StatusCode, result)
                : StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetPatientByIdQuery.Query { Id = id };
            var result = await _getPatient.Handle(query);

            return result.Success
                ? Ok(result)
                : StatusCode(result.StatusCode, result);
        }
    }
}
