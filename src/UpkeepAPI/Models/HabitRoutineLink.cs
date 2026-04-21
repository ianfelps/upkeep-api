namespace UpkeepAPI.Models;

public class HabitRoutineLink : BaseEntity
{
    public Guid HabitId { get; set; }
    public Habit Habit { get; set; } = null!;
    public Guid RoutineEventId { get; set; }
    public RoutineEvent RoutineEvent { get; set; } = null!;
}
