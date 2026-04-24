using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UpkeepAPI.DTOs.Habit;
using UpkeepAPI.Services.Interfaces;

namespace UpkeepAPI.Controllers;

[ApiController]
[Route("habits")]
[Authorize]
[Produces("application/json")]
[SwaggerTag("Gerenciamento dos hábitos do usuário autenticado")]
public class HabitsController : ControllerBase
{
    private readonly IHabitService _habitService;

    public HabitsController(IHabitService habitService)
    {
        _habitService = habitService;
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
        Summary = "Listar hábitos do usuário autenticado",
        Description = """
            Retorna os hábitos do usuário.

            **Sincronização offline-first:** informe `updatedSince` (ISO 8601, UTC) para buscar apenas hábitos modificados após aquele momento.
            """
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Hábitos retornados com sucesso", typeof(List<HabitDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? updatedSince)
    {
        var userId = GetCurrentUserId();
        var habits = await _habitService.GetAllByUserAsync(userId, updatedSince);
        return Ok(habits);
    }

    [HttpGet("heatmap")]
    [SwaggerOperation(
        Summary = "Heatmap de todos os hábitos",
        Description = """
            Retorna a contagem de hábitos completados por dia no intervalo informado.

            **Padrão:** últimos 365 dias.

            Formato da resposta: `[{ date, completedCount, totalHabits }]` — apenas dias com ao menos um registro são retornados.
            """
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Heatmap retornado com sucesso", typeof(List<HabitHeatmapEntryDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    public async Task<IActionResult> GetHeatmap(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var userId = GetCurrentUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rangeFrom = from ?? today.AddDays(-364);
        var rangeTo = to ?? today;
        var heatmap = await _habitService.GetHeatmapAsync(userId, rangeFrom, rangeTo);
        return Ok(heatmap);
    }

    [HttpGet("{id:guid}", Name = "GetHabitById")]
    [SwaggerOperation(
        Summary = "Obter um hábito específico",
        Description = "Retorna o hábito identificado pelo `id`, caso pertença ao usuário autenticado."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Hábito retornado com sucesso", typeof(HabitDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Hábito não encontrado")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _habitService.GetByIdAsync(userId, id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("")]
    [SwaggerOperation(
        Summary = "Criar novo hábito",
        Description = "Cria um hábito vinculado ao usuário autenticado. Opcionalmente vincula a eventos de rotina via `routineEventIds`."
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Hábito criado com sucesso", typeof(HabitDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    public async Task<IActionResult> Create([FromBody] CreateHabitDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _habitService.CreateAsync(userId, dto);
            return CreatedAtRoute("GetHabitById", new { id = result.Id }, result);
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
        Summary = "Atualizar hábito",
        Description = "Substitui integralmente os dados do hábito. Os vínculos com eventos de rotina são substituídos atomicamente pelo array `routineEventIds` informado."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Hábito atualizado com sucesso", typeof(HabitDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Hábito não encontrado")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHabitDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _habitService.UpdateAsync(userId, id, dto);
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
        Summary = "Excluir hábito",
        Description = "Remove permanentemente o hábito e todos os seus registros."
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Hábito excluído com sucesso")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Hábito não encontrado")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _habitService.DeleteAsync(userId, id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
