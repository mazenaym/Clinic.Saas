using Clinic.Saas.Infrastructure.Data;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly DapperContext _context;

        public TestController(DapperContext context)
        {
            _context = context;
        }

        [HttpGet("connection")]
        public async Task<IActionResult> TestConnection()
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryFirstAsync<int>("SELECT COUNT(*) FROM dbo.Drugs");
            return Ok(new { DrugCount = result });
        }
    }
}
