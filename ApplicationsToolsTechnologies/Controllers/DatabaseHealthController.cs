
using ApplicationsToolsTechnologies.Data;
using Microsoft.AspNetCore.Mvc;

namespace ApplicationsToolsTechnologies.Controllers;

[ApiController]
[Route("api/health/database")]
public sealed class DatabaseHealthController : ControllerBase
{
    private readonly DatabaseConnectionTest _connectionTest;

    public DatabaseHealthController(DatabaseConnectionTest connectionTest)
    {
        _connectionTest = connectionTest;
    }

    [HttpGet]
    public async Task<IActionResult> CheckDatabase()
    {
        try
        {
            var databaseName = await _connectionTest.TestAsync();

            return Ok(new
            {
                status = "Connected",
                database = databaseName
            });
        }

        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                status = "Database connection failed",
                errorType = ex.GetType().Name,
                message = ex.Message
            });
        }

    }
}
