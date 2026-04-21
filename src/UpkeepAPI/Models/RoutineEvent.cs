namespace UpkeepAPI.Models;

public class RoutineEvent : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int[] DaysOfWeek { get; set; } = Array.Empty<int>();
    public bool IsActive { get; set; } = true;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
