namespace UpkeepAPI.DTOs.RoutineEvent;

public class RoutineEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int[]? DaysOfWeek { get; set; }
    public DateOnly? EventDate { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Color { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
