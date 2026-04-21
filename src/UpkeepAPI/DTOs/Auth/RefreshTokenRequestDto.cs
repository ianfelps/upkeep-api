using System.ComponentModel.DataAnnotations;

namespace UpkeepAPI.DTOs.Auth;

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "O refresh token é obrigatório.")]
    public string RefreshToken { get; set; } = string.Empty;
}
