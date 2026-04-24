using System.ComponentModel.DataAnnotations;

namespace UpkeepAPI.DTOs.User;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "A senha atual é obrigatória.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "A nova senha é obrigatória.")]
    [MinLength(8, ErrorMessage = "A nova senha deve ter no mínimo 8 caracteres.")]
    [MaxLength(72, ErrorMessage = "A nova senha deve ter no máximo 72 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;
}
