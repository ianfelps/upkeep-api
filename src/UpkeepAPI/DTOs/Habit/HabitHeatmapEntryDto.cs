namespace UpkeepAPI.DTOs.Habit;

public class HabitHeatmapEntryDto
{
    public DateOnly Date { get; set; }
    public int CompletedCount { get; set; }
    public int TotalHabits { get; set; }
}
