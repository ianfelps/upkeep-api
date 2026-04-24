using Microsoft.EntityFrameworkCore;
using UpkeepAPI.Data;
using UpkeepAPI.DTOs.UserProgress;
using UpkeepAPI.Mappers;
using UpkeepAPI.Models;
using UpkeepAPI.Services.Interfaces;

namespace UpkeepAPI.Services;

public class UserProgressService : IUserProgressService
{
    private readonly AppDbContext _context;
    private readonly IAchievementService _achievementService;

    public UserProgressService(AppDbContext context, IAchievementService achievementService)
    {
        _context = context;
        _achievementService = achievementService;
    }

    public async Task<UserProgressDto> GetProgressAsync(Guid userId)
    {
        var logs = await _context.HabitLogs
            .AsNoTracking()
            .Where(l => l.Habit.UserId == userId)
            .Select(l => new { l.TargetDate, l.Status, l.EarnedXP })
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var since30 = today.AddDays(-29);
        var since7 = today.AddDays(-6);

        var recentLogs = await _context.HabitLogs
            .AsNoTracking()
            .Where(l => l.Habit.UserId == userId && l.TargetDate >= since30)
            .Select(l => new { l.TargetDate, l.Status })
            .ToListAsync();

        int totalHabitsActive = await _context.Habits
            .CountAsync(h => h.UserId == userId && h.IsActive);

        int totalHabits = await _context.Habits
            .CountAsync(h => h.UserId == userId);

        int totalXP = logs.Where(l => l.Status == HabitStatus.Completed).Sum(l => l.EarnedXP);
        int currentLevel = (int)Math.Floor(Math.Sqrt(totalXP / 50.0)) + 1;

        var lastDate = logs
            .Where(l => l.Status == HabitStatus.Completed)
            .Select(l => l.TargetDate)
            .OrderByDescending(d => d)
            .FirstOrDefault();
        DateTime? lastActivity = lastDate == default
            ? null
            : lastDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var completedDates = logs.Where(l => l.Status == HabitStatus.Completed).Select(l => l.TargetDate);
        int currentStreak = ComputeCurrentStreak(completedDates);
        int longestStreak = ComputeLongestStreak(completedDates);

        int totalLogsCompleted = logs.Count(l => l.Status == HabitStatus.Completed);

        var logs7 = recentLogs.Where(l => l.TargetDate >= since7).ToList();
        decimal rate7 = logs7.Count == 0
            ? 0m
            : Math.Round((decimal)logs7.Count(l => l.Status == HabitStatus.Completed) / logs7.Count, 3);
        decimal rate30 = recentLogs.Count == 0
            ? 0m
            : Math.Round((decimal)recentLogs.Count(l => l.Status == HabitStatus.Completed) / recentLogs.Count, 3);

        var progress = await _context.UserProgress.FirstOrDefaultAsync(up => up.UserId == userId);
        if (progress is null)
        {
            progress = new UserProgress { UserId = userId };
            _context.UserProgress.Add(progress);
        }

        progress.TotalXP = totalXP;
        progress.CurrentLevel = currentLevel;
        progress.CurrentStreak = currentStreak;
        progress.LongestStreak = longestStreak;
        progress.LastActivity = lastActivity;

        await _context.SaveChangesAsync();

        await _achievementService.CheckAndUnlockAsync(userId, new AchievementStats(
            LongestStreak: longestStreak,
            TotalLogsCompleted: totalLogsCompleted,
            CurrentLevel: currentLevel,
            TotalHabits: totalHabits
        ));

        return progress.ToDto(totalHabitsActive, totalLogsCompleted, rate7, rate30);
    }

    public async Task SeedAsync(Guid userId)
    {
        if (await _context.UserProgress.AnyAsync(up => up.UserId == userId)) return;

        _context.UserProgress.Add(new UserProgress { UserId = userId, CurrentLevel = 1 });
        await _context.SaveChangesAsync();
    }

    private static int ComputeCurrentStreak(IEnumerable<DateOnly> completedDates)
    {
        var days = completedDates.Distinct().OrderByDescending(d => d).ToList();
        if (days.Count == 0) return 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (days[0] != today && days[0] != today.AddDays(-1)) return 0;

        int streak = 1;
        for (int i = 1; i < days.Count; i++)
        {
            if (days[i - 1].AddDays(-1) == days[i]) streak++;
            else break;
        }
        return streak;
    }

    private static int ComputeLongestStreak(IEnumerable<DateOnly> completedDates)
    {
        var days = completedDates.Distinct().OrderBy(d => d).ToList();
        if (days.Count == 0) return 0;

        int longest = 1, current = 1;
        for (int i = 1; i < days.Count; i++)
        {
            if (days[i] == days[i - 1].AddDays(1))
            {
                current++;
                if (current > longest) longest = current;
            }
            else
            {
                current = 1;
            }
        }
        return longest;
    }
}
