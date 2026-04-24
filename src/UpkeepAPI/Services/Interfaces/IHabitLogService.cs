using UpkeepAPI.DTOs.HabitLog;

namespace UpkeepAPI.Services.Interfaces;

public interface IHabitLogService
{
    Task<List<HabitLogDto>> GetAllByHabitAsync(Guid userId, Guid habitId, DateTime? updatedSince, DateOnly? from, DateOnly? to);
    Task<HabitLogDto> GetByIdAsync(Guid userId, Guid habitId, Guid logId);
    Task<HabitLogDto> CreateAsync(Guid userId, Guid habitId, CreateHabitLogDto dto);
    Task<HabitLogDto> UpdateAsync(Guid userId, Guid habitId, Guid logId, UpdateHabitLogDto dto);
    Task DeleteAsync(Guid userId, Guid habitId, Guid logId);
}
