using Microsoft.EntityFrameworkCore;
using UpkeepAPI.Data;
using UpkeepAPI.DTOs.HabitLog;
using UpkeepAPI.Mappers;
using UpkeepAPI.Models;
using UpkeepAPI.Services.Interfaces;

namespace UpkeepAPI.Services;

public class HabitLogService : IHabitLogService
{
    private readonly AppDbContext _context;

    public HabitLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<HabitLogDto>> GetAllByHabitAsync(Guid userId, Guid habitId, DateTime? updatedSince, DateOnly? from, DateOnly? to)
    {
        await EnsureHabitOwnership(userId, habitId);

        var query = _context.HabitLogs.AsNoTracking().Where(l => l.HabitId == habitId);

        if (updatedSince.HasValue)
        {
            var since = DateTime.SpecifyKind(updatedSince.Value, DateTimeKind.Utc);
            var logs = await query.Where(l => l.UpdatedAt > since).OrderBy(l => l.TargetDate).ToListAsync();
            return logs.Select(l => l.ToDto()).ToList();
        }

        var rangeFrom = from ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var rangeTo = to ?? rangeFrom;

        var result = await query
            .Where(l => l.TargetDate >= rangeFrom && l.TargetDate <= rangeTo)
            .OrderBy(l => l.TargetDate)
            .ToListAsync();

        return result.Select(l => l.ToDto()).ToList();
    }

    public async Task<HabitLogDto> GetByIdAsync(Guid userId, Guid habitId, Guid logId)
    {
        var log = await FindOwnedLogAsync(userId, habitId, logId);
        return log.ToDto();
    }

    public async Task<HabitLogDto> CreateAsync(Guid userId, Guid habitId, CreateHabitLogDto dto)
    {
        await EnsureHabitOwnership(userId, habitId);

        var duplicate = await _context.HabitLogs
            .AnyAsync(l => l.HabitId == habitId && l.TargetDate == dto.TargetDate);

        if (duplicate)
            throw new InvalidOperationException($"Já existe um registro para o hábito na data {dto.TargetDate:yyyy-MM-dd}.");

        var log = new HabitLog
        {
            HabitId = habitId,
            TargetDate = dto.TargetDate,
            Status = dto.Status,
            CompletedAt = dto.Status == HabitStatus.Completed ? DateTime.UtcNow : null,
            Notes = dto.Notes?.Trim(),
            EarnedXP = dto.EarnedXP
        };

        _context.HabitLogs.Add(log);
        await _context.SaveChangesAsync();

        return log.ToDto();
    }

    public async Task<HabitLogDto> UpdateAsync(Guid userId, Guid habitId, Guid logId, UpdateHabitLogDto dto)
    {
        var log = await FindOwnedLogAsync(userId, habitId, logId);

        var wasCompleted = log.Status == HabitStatus.Completed;
        var isNowCompleted = dto.Status == HabitStatus.Completed;

        log.Status = dto.Status;
        log.Notes = dto.Notes?.Trim();
        log.EarnedXP = dto.EarnedXP;

        if (!wasCompleted && isNowCompleted)
            log.CompletedAt = DateTime.UtcNow;
        else if (wasCompleted && !isNowCompleted)
            log.CompletedAt = null;

        await _context.SaveChangesAsync();

        return log.ToDto();
    }

    public async Task DeleteAsync(Guid userId, Guid habitId, Guid logId)
    {
        var log = await FindOwnedLogAsync(userId, habitId, logId);
        _context.HabitLogs.Remove(log);
        await _context.SaveChangesAsync();
    }

    private async Task EnsureHabitOwnership(Guid userId, Guid habitId)
    {
        var owned = await _context.Habits.AnyAsync(h => h.Id == habitId && h.UserId == userId);
        if (!owned)
            throw new KeyNotFoundException("Hábito não encontrado.");
    }

    private async Task<HabitLog> FindOwnedLogAsync(Guid userId, Guid habitId, Guid logId)
    {
        await EnsureHabitOwnership(userId, habitId);

        var log = await _context.HabitLogs
            .FirstOrDefaultAsync(l => l.Id == logId && l.HabitId == habitId);

        return log ?? throw new KeyNotFoundException("Registro de hábito não encontrado.");
    }
}
