namespace UpkeepAPI.DTOs.UserProgress;

public class UserProgressDto
{
    public int CurrentLevel { get; set; }
    public int TotalXP { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastActivity { get; set; }
    public int TotalHabitsActive { get; set; }
    public int TotalLogsCompleted { get; set; }
    public decimal CompletionRateLast7Days { get; set; }
    public decimal CompletionRateLast30Days { get; set; }
    public DateTime UpdatedAt { get; set; }
}
