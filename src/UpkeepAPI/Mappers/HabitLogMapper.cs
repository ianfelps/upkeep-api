using UpkeepAPI.DTOs.HabitLog;
using UpkeepAPI.Models;

namespace UpkeepAPI.Mappers;

public static class HabitLogMapper
{
    public static HabitLogDto ToDto(this HabitLog log) => new()
    {
        Id = log.Id,
        TargetDate = log.TargetDate,
        CompletedAt = log.CompletedAt,
        Status = log.Status.ToString(),
        Notes = log.Notes,
        EarnedXP = log.EarnedXP,
        HabitId = log.HabitId,
        CreatedAt = log.CreatedAt,
        UpdatedAt = log.UpdatedAt
    };
}
