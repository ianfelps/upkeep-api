using UpkeepAPI.DTOs.RoutineEvent;

namespace UpkeepAPI.Services.Interfaces;

public interface IRoutineEventService
{
    Task<List<RoutineEventDto>> GetAllByUserAsync(Guid userId, DateTime? updatedSince);
    Task<RoutineEventDto> GetByIdAsync(Guid userId, Guid id);
    Task<RoutineEventDto> CreateAsync(Guid userId, CreateRoutineEventDto dto);
    Task<RoutineEventDto> UpdateAsync(Guid userId, Guid id, UpdateRoutineEventDto dto);
    Task DeleteAsync(Guid userId, Guid id);
}
