using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UpkeepAPI.DTOs.User;
using UpkeepAPI.DTOs.UserProgress;
using UpkeepAPI.Services.Interfaces;

namespace UpkeepAPI.Controllers;

[ApiController]
[Route("users")]
[Authorize]
[Produces("application/json")]
[SwaggerTag("Gerenciamento do próprio usuário autenticado")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserProgressService _userProgressService;
    private readonly IAchievementService _achievementService;

    public UsersController(IUserService userService, IUserProgressService userProgressService, IAchievementService achievementService)
    {
        _userService = userService;
        _userProgressService = userProgressService;
        _achievementService = achievementService;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Token inválido.");

        return Guid.Parse(claim);
    }

    [HttpGet("me")]
    [SwaggerOperation(
        Summary = "Obter dados do usuário autenticado",
        Description = "Retorna as informações do usuário correspondente ao token JWT informado."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Dados retornados com sucesso", typeof(UserDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Usuário não encontrado")]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetByIdAsync(userId);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("me")]
    [SwaggerOperation(
        Summary = "Atualizar nome e e-mail do usuário autenticado",
        Description = "Substitui o nome e o e-mail do usuário. Todos os campos são obrigatórios."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Dados atualizados com sucesso", typeof(UserDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Usuário não encontrado")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "E-mail já está em uso por outro usuário")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userService.UpdateAsync(userId, dto);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("me/password")]
    [SwaggerOperation(
        Summary = "Alterar senha do usuário autenticado",
        Description = "Exige a senha atual para confirmação antes de definir a nova senha."
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Senha alterada com sucesso")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Senha atual incorreta ou dados inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Usuário não encontrado")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _userService.ChangePasswordAsync(userId, dto);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me/progress")]
    [SwaggerOperation(
        Summary = "Obter progresso do usuário autenticado",
        Description = "Retorna estatísticas computadas de XP, nível, sequências e taxa de conclusão."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Progresso retornado com sucesso", typeof(UserProgressDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    public async Task<IActionResult> GetMyProgress()
    {
        var userId = GetCurrentUserId();
        var progress = await _userProgressService.GetProgressAsync(userId);
        return Ok(progress);
    }

    [HttpGet("me/achievements")]
    [SwaggerOperation(
        Summary = "Obter conquistas do usuário autenticado",
        Description = "Retorna todas as conquistas disponíveis, indicando quais foram desbloqueadas e quando."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Conquistas retornadas com sucesso", typeof(List<AchievementDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    public async Task<IActionResult> GetMyAchievements()
    {
        var userId = GetCurrentUserId();
        var achievements = await _achievementService.GetAllAsync(userId);
        return Ok(achievements);
    }

    [HttpDelete("me")]
    [SwaggerOperation(
        Summary = "Excluir conta do usuário autenticado",
        Description = "Remove permanentemente a conta do usuário. Esta ação não pode ser desfeita."
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Conta excluída com sucesso")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token ausente ou inválido")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Usuário não encontrado")]
    public async Task<IActionResult> DeleteMe()
    {
        try
        {
            var userId = GetCurrentUserId();
            await _userService.DeleteAsync(userId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
