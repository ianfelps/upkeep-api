using UpkeepAPI.DTOs.UserProgress;
using UpkeepAPI.Models;

namespace UpkeepAPI.Mappers;

public static class UserProgressMapper
{
    public static UserProgressDto ToDto(
        this UserProgress p,
        int totalHabitsActive,
        int totalLogsCompleted,
        decimal completionRateLast7Days,
        decimal completionRateLast30Days) => new()
    {
        CurrentLevel = p.CurrentLevel,
        TotalXP = p.TotalXP,
        CurrentStreak = p.CurrentStreak,
        LongestStreak = p.LongestStreak,
        LastActivity = p.LastActivity,
        TotalHabitsActive = totalHabitsActive,
        TotalLogsCompleted = totalLogsCompleted,
        CompletionRateLast7Days = completionRateLast7Days,
        CompletionRateLast30Days = completionRateLast30Days,
        UpdatedAt = p.UpdatedAt
    };
}
