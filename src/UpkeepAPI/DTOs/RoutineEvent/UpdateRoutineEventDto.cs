using System.ComponentModel.DataAnnotations;

namespace UpkeepAPI.DTOs.RoutineEvent;

public class UpdateRoutineEventDto : IValidatableObject
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime.HasValue && EndTime.Value <= StartTime)
            yield return new ValidationResult(
                "A hora de término deve ser maior que a hora de início.",
                [nameof(EndTime)]);

        if (DaysOfWeek.Any(d => d < 0 || d > 6))
            yield return new ValidationResult(
                "Os dias da semana devem ser valores entre 0 (domingo) e 6 (sábado).",
                [nameof(DaysOfWeek)]);
    }
}
