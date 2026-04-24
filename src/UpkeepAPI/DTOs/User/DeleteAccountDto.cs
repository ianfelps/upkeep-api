using System.ComponentModel.DataAnnotations;

namespace UpkeepAPI.DTOs.User;

public class DeleteAccountDto
{
    [Required(ErrorMessage = "A senha atual é obrigatória.")]
    public string CurrentPassword { get; set; } = string.Empty;
}
