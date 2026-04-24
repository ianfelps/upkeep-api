using Microsoft.EntityFrameworkCore;
using UpkeepAPI.Data;
using UpkeepAPI.DTOs.Habit;
using UpkeepAPI.Mappers;
using UpkeepAPI.Models;
using UpkeepAPI.Services.Interfaces;

namespace UpkeepAPI.Services;

public class HabitService : IHabitService
{
    private readonly AppDbContext _context;

    public HabitService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<HabitDto>> GetAllByUserAsync(Guid userId, DateTime? updatedSince)
    {
        var query = _context.Habits.AsNoTracking().Where(h => h.UserId == userId);

        if (updatedSince.HasValue)
        {
            var since = DateTime.SpecifyKind(updatedSince.Value, DateTimeKind.Utc);
            query = query.Where(h => h.UpdatedAt > since);
        }

        var habits = await query.OrderBy(h => h.CreatedAt).ToListAsync();

        var habitIds = habits.Select(h => h.Id).ToList();
        var links = await _context.HabitRoutineLinks
            .AsNoTracking()
            .Where(l => habitIds.Contains(l.HabitId))
            .ToListAsync();

        var linksByHabit = links.GroupBy(l => l.HabitId)
            .ToDictionary(g => g.Key, g => g.Select(l => l.RoutineEventId));

        return habits.Select(h =>
            h.ToDto(linksByHabit.TryGetValue(h.Id, out var ids) ? ids : [])).ToList();
    }

    public async Task<HabitDto> GetByIdAsync(Guid userId, Guid id)
    {
        var habit = await FindOwnedAsync(userId, id);
        var linkedIds = await GetLinkedRoutineEventIds(habit.Id);
        return habit.ToDto(linkedIds);
    }

    public async Task<HabitDto> CreateAsync(Guid userId, CreateHabitDto dto)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            throw new KeyNotFoundException("Usuário não encontrado.");

        if (dto.RoutineEventIds is { Length: > 0 })
            await ValidateRoutineEventOwnership(userId, dto.RoutineEventIds);

        var habit = new Habit
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            LucideIcon = dto.LucideIcon.Trim(),
            Color = dto.Color.Trim(),
            FrequencyType = dto.FrequencyType,
            TargetValue = dto.TargetValue,
            IsActive = true,
            UserId = userId
        };

        _context.Habits.Add(habit);

        if (dto.RoutineEventIds is { Length: > 0 })
        {
            foreach (var routineEventId in dto.RoutineEventIds.Distinct())
                _context.HabitRoutineLinks.Add(new HabitRoutineLink { HabitId = habit.Id, RoutineEventId = routineEventId });
        }

        await _context.SaveChangesAsync();

        var linkedIds = await GetLinkedRoutineEventIds(habit.Id);
        return habit.ToDto(linkedIds);
    }

    public async Task<HabitDto> UpdateAsync(Guid userId, Guid id, UpdateHabitDto dto)
    {
        var habit = await FindOwnedAsync(userId, id);

        if (dto.RoutineEventIds is { Length: > 0 })
            await ValidateRoutineEventOwnership(userId, dto.RoutineEventIds);

        habit.Title = dto.Title.Trim();
        habit.Description = dto.Description.Trim();
        habit.LucideIcon = dto.LucideIcon.Trim();
        habit.Color = dto.Color.Trim();
        habit.FrequencyType = dto.FrequencyType;
        habit.TargetValue = dto.TargetValue;
        habit.IsActive = dto.IsActive;

        var existingLinks = await _context.HabitRoutineLinks
            .Where(l => l.HabitId == habit.Id)
            .ToListAsync();
        _context.HabitRoutineLinks.RemoveRange(existingLinks);

        if (dto.RoutineEventIds is { Length: > 0 })
        {
            foreach (var routineEventId in dto.RoutineEventIds.Distinct())
                _context.HabitRoutineLinks.Add(new HabitRoutineLink { HabitId = habit.Id, RoutineEventId = routineEventId });
        }

        await _context.SaveChangesAsync();

        var linkedIds = await GetLinkedRoutineEventIds(habit.Id);
        return habit.ToDto(linkedIds);
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var habit = await FindOwnedAsync(userId, id);
        _context.Habits.Remove(habit);
        await _context.SaveChangesAsync();
    }

    public async Task<List<HabitHeatmapEntryDto>> GetHeatmapAsync(Guid userId, DateOnly from, DateOnly to)
    {
        var totalHabits = await _context.Habits
            .AsNoTracking()
            .CountAsync(h => h.UserId == userId && h.IsActive);

        var logs = await _context.HabitLogs
            .AsNoTracking()
            .Where(l => l.Habit.UserId == userId && l.TargetDate >= from && l.TargetDate <= to)
            .Select(l => new { l.TargetDate, l.Status })
            .ToListAsync();

        var grouped = logs
            .GroupBy(l => l.TargetDate)
            .Select(g => new HabitHeatmapEntryDto
            {
                Date = g.Key,
                CompletedCount = g.Count(l => l.Status == Models.HabitStatus.Completed),
                TotalHabits = totalHabits
            })
            .OrderBy(e => e.Date)
            .ToList();

        return grouped;
    }

    private async Task<Habit> FindOwnedAsync(Guid userId, Guid id)
    {
        var habit = await _context.Habits
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);

        return habit ?? throw new KeyNotFoundException("Hábito não encontrado.");
    }

    private async Task<IEnumerable<Guid>> GetLinkedRoutineEventIds(Guid habitId)
    {
        return await _context.HabitRoutineLinks
            .AsNoTracking()
            .Where(l => l.HabitId == habitId)
            .Select(l => l.RoutineEventId)
            .ToListAsync();
    }

    private async Task ValidateRoutineEventOwnership(Guid userId, Guid[] routineEventIds)
    {
        var distinctIds = routineEventIds.Distinct().ToList();
        var ownedCount = await _context.RoutineEvents
            .AsNoTracking()
            .CountAsync(re => distinctIds.Contains(re.Id) && re.UserId == userId);

        if (ownedCount != distinctIds.Count)
            throw new InvalidOperationException("Um ou mais eventos de rotina não pertencem ao usuário.");
    }
}
