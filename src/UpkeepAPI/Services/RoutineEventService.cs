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

    public async Task<List<RoutineEventDto>> GetAllByUserAsync(Guid userId, DateTime? updatedSince, DateOnly? from, DateOnly? to)
    {
        var query = _context.RoutineEvents.AsNoTracking().Where(re => re.UserId == userId);

        if (updatedSince.HasValue)
        {
            var since = DateTime.SpecifyKind(updatedSince.Value, DateTimeKind.Utc);
            query = query.Where(re => re.UpdatedAt > since);

            // Delta sync: return all matching events without date filter
            var allChanged = await query.OrderBy(re => re.StartTime).ToListAsync();
            return allChanged.Select(re => re.ToDto()).ToList();
        }

        // Default: filter by date range (or today if none provided)
        var rangeFrom = from ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var rangeTo = to ?? rangeFrom;

        var daysInRange = GetDaysOfWeekInRange(rangeFrom, rangeTo);

        query = query.Where(re =>
            (re.EventDate.HasValue && re.EventDate >= rangeFrom && re.EventDate <= rangeTo) ||
            (!re.EventDate.HasValue && re.DaysOfWeek != null &&
             re.DaysOfWeek.Any(d => daysInRange.Contains(d))));

        var events = await query.OrderBy(re => re.StartTime).ToListAsync();
        return events.Select(re => re.ToDto()).ToList();
    }

    private static HashSet<int> GetDaysOfWeekInRange(DateOnly from, DateOnly to)
    {
        var days = new HashSet<int>();
        var current = from;
        while (current <= to && days.Count < 7)
        {
            days.Add((int)current.DayOfWeek);
            current = current.AddDays(1);
        }
        return days;
    }

    public async Task<RoutineEventDto> GetByIdAsync(Guid userId, Guid id)
    {
        var routineEvent = await FindOwnedAsync(userId, id);
        return routineEvent.ToDto();
    }

    public async Task<RoutineEventDto> CreateAsync(Guid userId, CreateRoutineEventDto dto)
    {
        ValidateSchedule(dto.StartTime, dto.EndTime, dto.DaysOfWeek, dto.EventDate);

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            throw new KeyNotFoundException("Usuário não encontrado.");

        var routineEvent = new RoutineEvent
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            DaysOfWeek = dto.DaysOfWeek?.Distinct().OrderBy(d => d).ToArray(),
            EventDate = dto.EventDate,
            IsActive = dto.IsActive,
            UserId = userId
        };

        _context.RoutineEvents.Add(routineEvent);
        await _context.SaveChangesAsync();

        return routineEvent.ToDto();
    }

    public async Task<RoutineEventDto> UpdateAsync(Guid userId, Guid id, UpdateRoutineEventDto dto)
    {
        ValidateSchedule(dto.StartTime, dto.EndTime, dto.DaysOfWeek, dto.EventDate);

        var routineEvent = await FindOwnedAsync(userId, id);

        routineEvent.Title = dto.Title.Trim();
        routineEvent.Description = dto.Description?.Trim();
        routineEvent.StartTime = dto.StartTime;
        routineEvent.EndTime = dto.EndTime;
        routineEvent.DaysOfWeek = dto.DaysOfWeek?.Distinct().OrderBy(d => d).ToArray();
        routineEvent.EventDate = dto.EventDate;
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

    private static void ValidateSchedule(TimeSpan startTime, TimeSpan? endTime, int[]? daysOfWeek, DateOnly? eventDate)
    {
        var hasDays = daysOfWeek is { Length: > 0 };
        var hasDate = eventDate.HasValue;

        if (hasDays == hasDate)
            throw new InvalidOperationException("Informe exatamente um de: dias da semana (recorrente) ou data específica (evento único).");

        if (hasDays && daysOfWeek!.Any(d => d < 0 || d > 6))
            throw new InvalidOperationException("Dias da semana devem estar entre 0 (domingo) e 6 (sábado).");

        if (endTime.HasValue && endTime.Value <= startTime)
            throw new InvalidOperationException("A hora de fim deve ser maior que a hora de início.");
    }
}
