using System.ComponentModel.DataAnnotations;

namespace UpkeepAPI.DTOs.RoutineEvent;

public class UpdateRoutineEventDto
{
    [Required(ErrorMessage = "O título é obrigatório.")]
    [MaxLength(100, ErrorMessage = "O título deve ter no máximo 100 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "A hora de início é obrigatória.")]
    public TimeSpan StartTime { get; set; }

    public TimeSpan? EndTime { get; set; }

    [Required(ErrorMessage = "Os dias da semana são obrigatórios.")]
    [MinLength(1, ErrorMessage = "Informe ao menos um dia da semana.")]
    public int[] DaysOfWeek { get; set; } = Array.Empty<int>();

    public bool IsActive { get; set; } = true;
}
