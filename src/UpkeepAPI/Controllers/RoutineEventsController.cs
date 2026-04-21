using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UpkeepAPI.DTOs.RoutineEvent;
using UpkeepAPI.Services.Interfaces;

namespace UpkeepAPI.Controllers;

[ApiController]
[Route("routine-events")]
[Authorize]
[Produces("application/json")]
[SwaggerTag("Gerenciamento dos eventos de rotina do usuário autenticado")]
public class RoutineEventsController : ControllerBase
{
    private readonly IRoutineEventService _routineEventService;

    public RoutineEventsController(IRoutineEventService routineEventService)
    {
        _routineEventService = routineEventService;
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
        Summary = "Listar eventos de rotina do usuário autenticado",
        Description = """
            Retorna eventos de rotina do usuário filtrados por intervalo de datas.

            **Comportamento padrão (sem `from`/`to`):** retorna os eventos do dia atual.

            **Filtro por intervalo:** use `from` e `to` (formato `YYYY-MM-DD`) para buscar por dia, semana ou mês.
            - Eventos recorrentes: retornados se algum dia da semana ocorre no intervalo.
            - Eventos únicos: retornados se a data específica está no intervalo.

            **Sincronização offline-first:** informe `updatedSince` (ISO 8601, UTC) para buscar todos os eventos modificados após aquele momento, sem filtro de data.
            """
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Eventos retornados com sucesso", typeof(List<RoutineEventDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? updatedSince,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var userId = GetCurrentUserId();
        var events = await _routineEventService.GetAllByUserAsync(userId, updatedSince, from, to);
        return Ok(events);
    }

    [HttpGet("{id:guid}", Name = "GetRoutineEventById")]
    [SwaggerOperation(
        Summary = "Obter um evento de rotina específico",
        Description = "Retorna o evento de rotina identificado pelo `id`, caso pertença ao usuário autenticado."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Evento retornado com sucesso", typeof(RoutineEventDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Evento não encontrado")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _routineEventService.GetByIdAsync(userId, id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("")]
    [SwaggerOperation(
        Summary = "Criar novo evento de rotina",
        Description = "Cria um evento de rotina vinculado ao usuário autenticado."
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Evento criado com sucesso", typeof(RoutineEventDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    public async Task<IActionResult> Create([FromBody] CreateRoutineEventDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _routineEventService.CreateAsync(userId, dto);
            return CreatedAtRoute("GetRoutineEventById", new { id = result.Id }, result);
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

    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        Summary = "Atualizar evento de rotina",
        Description = "Substitui integralmente os dados do evento de rotina identificado pelo `id`."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Evento atualizado com sucesso", typeof(RoutineEventDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Evento não encontrado")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoutineEventDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _routineEventService.UpdateAsync(userId, id, dto);
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

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Excluir evento de rotina",
        Description = "Remove permanentemente o evento de rotina identificado pelo `id`."
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Evento excluído com sucesso")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Evento não encontrado")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _routineEventService.DeleteAsync(userId, id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
