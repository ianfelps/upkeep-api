namespace UpkeepAPI.Models;

public class HabitLog : BaseEntity
{
    public DateOnly TargetDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public HabitStatus Status { get; set; }
    public string? Notes { get; set; }
    public int EarnedXP { get; set; }
    public Guid HabitId { get; set; }
    public Habit Habit { get; set; } = null!;
}
