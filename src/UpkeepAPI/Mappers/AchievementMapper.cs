using UpkeepAPI.DTOs.UserProgress;
using UpkeepAPI.Models;

namespace UpkeepAPI.Mappers;

public static class AchievementMapper
{
    public static AchievementDto ToDto(this AchievementDefinition def, UserAchievement? ua) => new()
    {
        Key = def.Key.ToString(),
        Title = def.Title,
        Description = def.Description,
        Icon = def.Icon,
        IsUnlocked = ua is not null,
        UnlockedAt = ua?.CreatedAt
    };
}
