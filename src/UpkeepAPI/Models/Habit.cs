namespace UpkeepAPI.Models;

public class Habit : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LucideIcon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public HabitFrequencyType FrequencyType { get; set; }
    public int TargetValue { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
