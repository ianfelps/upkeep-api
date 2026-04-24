using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UpkeepAPI.DTOs.HabitLog;
using UpkeepAPI.Services.Interfaces;

namespace UpkeepAPI.Controllers;

[ApiController]
[Route("habits/{habitId:guid}/logs")]
[Authorize]
[Produces("application/json")]
[SwaggerTag("Registros de execução dos hábitos do usuário autenticado")]
public class HabitLogsController : ControllerBase
{
    private readonly IHabitLogService _habitLogService;

    public HabitLogsController(IHabitLogService habitLogService)
    {
        _habitLogService = habitLogService;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Token inválido.");

        return Guid.Parse(claim);
    }

    [HttpGet("")]
    [SwaggerOperation(
        Summary = "Listar registros do hábito",
        Description = """
            Retorna os registros de execução do hábito filtrados por data.

            **Padrão (sem `from`/`to`):** retorna registros do dia atual.

            **Sincronização offline-first:** informe `updatedSince` (ISO 8601, UTC) para buscar registros modificados após aquele momento, sem filtro de data. Serve como fonte de dados para o heatmap individual do hábito.
            """
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Registros retornados com sucesso", typeof(List<HabitLogDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Hábito não encontrado")]
    public async Task<IActionResult> GetAll(
        Guid habitId,
        [FromQuery] DateTime? updatedSince,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        try
        {
            var userId = GetCurrentUserId();
            var logs = await _habitLogService.GetAllByHabitAsync(userId, habitId, updatedSince, from, to);
            return Ok(logs);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{logId:guid}", Name = "GetHabitLogById")]
    [SwaggerOperation(
        Summary = "Obter um registro específico",
        Description = "Retorna o registro identificado pelo `logId`, caso pertença ao hábito e ao usuário autenticado."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Registro retornado com sucesso", typeof(HabitLogDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Registro não encontrado")]
    public async Task<IActionResult> GetById(Guid habitId, Guid logId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _habitLogService.GetByIdAsync(userId, habitId, logId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("")]
    [SwaggerOperation(
        Summary = "Registrar execução do hábito",
        Description = "Cria um registro de execução (Completado, Ignorado ou Perdido) para uma data específica. Apenas um registro por data é permitido."
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Registro criado com sucesso", typeof(HabitLogDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos ou registro duplicado para a data")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Hábito não encontrado")]
    public async Task<IActionResult> Create(Guid habitId, [FromBody] CreateHabitLogDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _habitLogService.CreateAsync(userId, habitId, dto);
            return CreatedAtRoute("GetHabitLogById", new { habitId, logId = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{logId:guid}")]
    [SwaggerOperation(
        Summary = "Atualizar registro de hábito",
        Description = "Atualiza o status, notas e XP de um registro existente."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Registro atualizado com sucesso", typeof(HabitLogDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Registro não encontrado")]
    public async Task<IActionResult> Update(Guid habitId, Guid logId, [FromBody] UpdateHabitLogDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _habitLogService.UpdateAsync(userId, habitId, logId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{logId:guid}")]
    [SwaggerOperation(
        Summary = "Excluir registro de hábito",
        Description = "Remove permanentemente o registro identificado pelo `logId`."
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Registro excluído com sucesso")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Registro não encontrado")]
    public async Task<IActionResult> Delete(Guid habitId, Guid logId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _habitLogService.DeleteAsync(userId, habitId, logId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
