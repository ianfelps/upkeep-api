using UpkeepAPI.DTOs.Habit;
using UpkeepAPI.Models;

namespace UpkeepAPI.Mappers;

public static class HabitMapper
{
    public static HabitDto ToDto(this Habit habit, IEnumerable<Guid> linkedRoutineEventIds) => new()
    {
        Id = habit.Id,
        Title = habit.Title,
        Description = habit.Description,
        Icon = habit.Icon,
        Color = habit.Color,
        FrequencyType = habit.FrequencyType.ToString(),
        TargetValue = habit.TargetValue,
        IsActive = habit.IsActive,
        UserId = habit.UserId,
        LinkedRoutineEventIds = linkedRoutineEventIds.ToArray(),
        CreatedAt = habit.CreatedAt,
        UpdatedAt = habit.UpdatedAt
    };
}
