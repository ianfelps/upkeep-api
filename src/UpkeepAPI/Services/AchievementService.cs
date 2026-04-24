using Microsoft.EntityFrameworkCore;
using UpkeepAPI.Data;
using UpkeepAPI.DTOs.UserProgress;
using UpkeepAPI.Mappers;
using UpkeepAPI.Models;
using UpkeepAPI.Services.Interfaces;

namespace UpkeepAPI.Services;

public class AchievementService : IAchievementService
{
    private readonly AppDbContext _context;

    public AchievementService(AppDbContext context)
    {
        _context = context;
    }

    public async Task CheckAndUnlockAsync(Guid userId, AchievementStats stats)
    {
        var unlocked = await _context.UserAchievements
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.Key)
            .ToListAsync();

        var toUnlock = Achievements.All
            .Where(def => !unlocked.Contains(def.Key) && Achievements.IsUnlocked(def.Key, stats))
            .Select(def => new UserAchievement { UserId = userId, Key = def.Key })
            .ToList();

        if (toUnlock.Count > 0)
        {
            _context.UserAchievements.AddRange(toUnlock);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<AchievementDto>> GetAllAsync(Guid userId)
    {
        var unlocked = await _context.UserAchievements
            .Where(ua => ua.UserId == userId)
            .ToListAsync();

        return Achievements.All
            .Select(def => def.ToDto(unlocked.FirstOrDefault(u => u.Key == def.Key)))
            .ToList();
    }
}
