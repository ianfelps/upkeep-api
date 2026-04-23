using System.ComponentModel.DataAnnotations;

namespace UpkeepAPI.DTOs.RoutineEvent;

public class CreateRoutineEventDto : IValidatableObject
{
    [Required(ErrorMessage = "O título é obrigatório.")]
    [MaxLength(100, ErrorMessage = "O título deve ter no máximo 100 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "A hora de início é obrigatória.")]
    public TimeSpan StartTime { get; set; }

    public TimeSpan? EndTime { get; set; }

    public int[]? DaysOfWeek { get; set; }

    public DateOnly? EventDate { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasDays = DaysOfWeek is { Length: > 0 };
        var hasDate = EventDate.HasValue;

        if (hasDays == hasDate)
            yield return new ValidationResult(
                "Informe exatamente um de: dias da semana (recorrente) ou data específica (evento único).",
                [nameof(DaysOfWeek), nameof(EventDate)]);

        if (hasDays && DaysOfWeek!.Any(d => d < 0 || d > 6))
            yield return new ValidationResult(
                "Os dias da semana devem ser valores entre 0 (domingo) e 6 (sábado).",
                [nameof(DaysOfWeek)]);

        if (EndTime.HasValue && EndTime.Value <= StartTime)
            yield return new ValidationResult(
                "A hora de término deve ser maior que a hora de início.",
                [nameof(EndTime)]);
    }
}
