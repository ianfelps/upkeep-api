namespace UpkeepAPI.DTOs.Habit;

public class HabitDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LucideIcon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string FrequencyType { get; set; } = string.Empty;
    public int TargetValue { get; set; }
    public bool IsActive { get; set; }
    public Guid UserId { get; set; }
    public Guid[] LinkedRoutineEventIds { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
