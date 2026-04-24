using UpkeepAPI.DTOs.Habit;

namespace UpkeepAPI.Services.Interfaces;

public interface IHabitService
{
    Task<List<HabitDto>> GetAllByUserAsync(Guid userId, DateTime? updatedSince);
    Task<HabitDto> GetByIdAsync(Guid userId, Guid id);
    Task<HabitDto> CreateAsync(Guid userId, CreateHabitDto dto);
    Task<HabitDto> UpdateAsync(Guid userId, Guid id, UpdateHabitDto dto);
    Task DeleteAsync(Guid userId, Guid id);
    Task<List<HabitHeatmapEntryDto>> GetHeatmapAsync(Guid userId, DateOnly from, DateOnly to);
}
