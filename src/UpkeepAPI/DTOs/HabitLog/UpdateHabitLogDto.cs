using System.ComponentModel.DataAnnotations;
using UpkeepAPI.Models;

namespace UpkeepAPI.DTOs.HabitLog;

public class UpdateHabitLogDto
{
    [Required(ErrorMessage = "O status é obrigatório.")]
    public HabitStatus Status { get; set; }

    [MaxLength(500, ErrorMessage = "As notas devem ter no máximo 500 caracteres.")]
    public string? Notes { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "O XP ganho deve ser maior ou igual a zero.")]
    public int EarnedXP { get; set; } = 0;
}
