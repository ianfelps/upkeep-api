using UpkeepAPI.DTOs.UserProgress;
using UpkeepAPI.Models;

namespace UpkeepAPI.Services.Interfaces;

public interface IAchievementService
{
    Task CheckAndUnlockAsync(Guid userId, AchievementStats stats);
    Task<List<AchievementDto>> GetAllAsync(Guid userId);
}
