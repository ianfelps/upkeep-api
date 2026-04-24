namespace UpkeepAPI.DTOs.HabitLog;

public class HabitLogDto
{
    public Guid Id { get; set; }
    public DateOnly TargetDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int EarnedXP { get; set; }
    public Guid HabitId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
