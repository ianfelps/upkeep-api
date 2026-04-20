using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;
using UpkeepAPI.Data;

namespace UpkeepAPI.Controllers;

[ApiController]
[Route("health")]
[Produces("application/json")]
[EnableRateLimiting("auth")]
[SwaggerTag("Status da aplicação")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;

    public HealthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Verificar status da aplicação",
        Description = "Retorna o status da API e a conectividade com o banco de dados."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Aplicação saudável")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Banco de dados inacessível")]
    public async Task<IActionResult> GetHealth()
    {
        var dbConnected = await _context.Database.CanConnectAsync();

        var response = new
        {
            status = dbConnected ? "healthy" : "degraded",
            database = dbConnected ? "connected" : "disconnected",
            timestamp = DateTime.UtcNow
        };

        return dbConnected ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
