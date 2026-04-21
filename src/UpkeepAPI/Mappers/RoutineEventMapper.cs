using UpkeepAPI.DTOs.RoutineEvent;
using UpkeepAPI.Models;

namespace UpkeepAPI.Mappers;

public static class RoutineEventMapper
{
    public static RoutineEventDto ToDto(this RoutineEvent routineEvent) => new()
    {
        Id = routineEvent.Id,
        Title = routineEvent.Title,
        Description = routineEvent.Description,
        StartTime = routineEvent.StartTime,
        EndTime = routineEvent.EndTime,
        DaysOfWeek = routineEvent.DaysOfWeek,
        EventDate = routineEvent.EventDate,
        EventType = routineEvent.EventDate.HasValue ? "once" : "recurring",
        IsActive = routineEvent.IsActive,
        UserId = routineEvent.UserId,
        CreatedAt = routineEvent.CreatedAt,
        UpdatedAt = routineEvent.UpdatedAt
    };
}
