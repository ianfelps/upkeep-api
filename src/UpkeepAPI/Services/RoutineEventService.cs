using Microsoft.EntityFrameworkCore;
using UpkeepAPI.Data;
using UpkeepAPI.DTOs.RoutineEvent;
using UpkeepAPI.Mappers;
using UpkeepAPI.Models;
using UpkeepAPI.Services.Interfaces;

namespace UpkeepAPI.Services;

public class RoutineEventService : IRoutineEventService
{
    private readonly AppDbContext _context;

    public RoutineEventService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoutineEventDto>> GetAllByUserAsync(Guid userId, DateTime? updatedSince)
    {
        var query = _context.RoutineEvents.AsNoTracking().Where(re => re.UserId == userId);

        if (updatedSince.HasValue)
        {
            var since = DateTime.SpecifyKind(updatedSince.Value, DateTimeKind.Utc);
            query = query.Where(re => re.UpdatedAt > since);
        }

        var events = await query.OrderBy(re => re.StartTime).ToListAsync();
        return events.Select(re => re.ToDto()).ToList();
    }

    public async Task<RoutineEventDto> GetByIdAsync(Guid userId, Guid id)
    {
        var routineEvent = await FindOwnedAsync(userId, id);
        return routineEvent.ToDto();
    }

    public async Task<RoutineEventDto> CreateAsync(Guid userId, CreateRoutineEventDto dto)
    {
        ValidateSchedule(dto.StartTime, dto.EndTime, dto.DaysOfWeek);

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            throw new KeyNotFoundException("Usuário não encontrado.");

        var routineEvent = new RoutineEvent
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            DaysOfWeek = dto.DaysOfWeek.Distinct().OrderBy(d => d).ToArray(),
            IsActive = dto.IsActive,
            UserId = userId
        };

        _context.RoutineEvents.Add(routineEvent);
        await _context.SaveChangesAsync();

        return routineEvent.ToDto();
    }

    public async Task<RoutineEventDto> UpdateAsync(Guid userId, Guid id, UpdateRoutineEventDto dto)
    {
        ValidateSchedule(dto.StartTime, dto.EndTime, dto.DaysOfWeek);

        var routineEvent = await FindOwnedAsync(userId, id);

        routineEvent.Title = dto.Title.Trim();
        routineEvent.Description = dto.Description?.Trim();
        routineEvent.StartTime = dto.StartTime;
        routineEvent.EndTime = dto.EndTime;
        routineEvent.DaysOfWeek = dto.DaysOfWeek.Distinct().OrderBy(d => d).ToArray();
        routineEvent.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return routineEvent.ToDto();
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var routineEvent = await FindOwnedAsync(userId, id);
        _context.RoutineEvents.Remove(routineEvent);
        await _context.SaveChangesAsync();
    }

    private async Task<RoutineEvent> FindOwnedAsync(Guid userId, Guid id)
    {
        var routineEvent = await _context.RoutineEvents
            .FirstOrDefaultAsync(re => re.Id == id && re.UserId == userId);

        return routineEvent
            ?? throw new KeyNotFoundException("Evento de rotina não encontrado.");
    }

    private static void ValidateSchedule(TimeSpan startTime, TimeSpan? endTime, int[] daysOfWeek)
    {
        if (daysOfWeek.Any(d => d < 0 || d > 6))
            throw new InvalidOperationException("Dias da semana devem estar entre 0 (domingo) e 6 (sábado).");

        if (endTime.HasValue && endTime.Value <= startTime)
            throw new InvalidOperationException("A hora de fim deve ser maior que a hora de início.");
    }
}
