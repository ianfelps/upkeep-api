using System.ComponentModel.DataAnnotations;
using UpkeepAPI.Models;

namespace UpkeepAPI.DTOs.Habit;

public class CreateHabitDto : IValidatableObject
{
    [Required(ErrorMessage = "O título é obrigatório.")]
    [MaxLength(100, ErrorMessage = "O título deve ter no máximo 100 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "O ícone deve ter no máximo 50 caracteres.")]
    public string Icon { get; set; } = string.Empty;

    [Required(ErrorMessage = "A cor é obrigatória.")]
    [MaxLength(7, ErrorMessage = "A cor deve ter no máximo 7 caracteres.")]
    public string Color { get; set; } = string.Empty;

    [Required(ErrorMessage = "O tipo de frequência é obrigatório.")]
    public HabitFrequencyType FrequencyType { get; set; }

    [Required(ErrorMessage = "O valor alvo é obrigatório.")]
    [Range(1, int.MaxValue, ErrorMessage = "O valor alvo deve ser maior que zero.")]
    public int TargetValue { get; set; }

    public Guid[]? RoutineEventIds { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.IsDefined(typeof(HabitFrequencyType), FrequencyType))
            yield return new ValidationResult(
                "O tipo de frequência informado é inválido.",
                [nameof(FrequencyType)]);

        if (RoutineEventIds is { Length: 0 })
            yield return new ValidationResult(
                "A lista de eventos de rotina não pode ser vazia. Omita o campo ou informe ao menos um ID.",
                [nameof(RoutineEventIds)]);
    }
}
