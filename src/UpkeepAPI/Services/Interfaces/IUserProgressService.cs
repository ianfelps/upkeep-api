using UpkeepAPI.DTOs.UserProgress;

namespace UpkeepAPI.Services.Interfaces;

public interface IUserProgressService
{
    Task<UserProgressDto> GetProgressAsync(Guid userId);
    Task SeedAsync(Guid userId);
}
