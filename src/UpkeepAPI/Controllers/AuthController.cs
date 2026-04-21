using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;
using UpkeepAPI.DTOs.Auth;
using UpkeepAPI.Services.Interfaces;

namespace UpkeepAPI.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
[EnableRateLimiting("auth")]
[SwaggerTag("Autenticação de usuários")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Cadastrar novo usuário",
        Description = "Cria uma nova conta e retorna o token JWT junto com os dados do usuário."
    )]
    [SwaggerResponse(StatusCodes.Status201Created, "Usuário criado com sucesso", typeof(AuthResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "E-mail já cadastrado")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto);
            return CreatedAtAction(nameof(Register), result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Entrar com e-mail e senha",
        Description = "Autentica o usuário e retorna o token JWT junto com os dados do usuário."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Login realizado com sucesso", typeof(AuthResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "E-mail ou senha incorretos")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [SwaggerOperation(
        Summary = "Renovar access token usando refresh token",
        Description = "Troca um refresh token válido por um novo par (access token + refresh token). O refresh token anterior é revogado — rotação em cada chamada."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Tokens renovados com sucesso", typeof(AuthResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Refresh token inválido, revogado ou expirado")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        try
        {
            var result = await _authService.RefreshAsync(dto.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [SwaggerOperation(
        Summary = "Revogar refresh token do dispositivo atual",
        Description = "Revoga o refresh token informado. O access token continua válido até expirar naturalmente. Idempotente: tokens desconhecidos retornam 204 sem erro."
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Refresh token revogado")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
    {
        await _authService.LogoutAsync(dto.RefreshToken);
        return NoContent();
    }
}
