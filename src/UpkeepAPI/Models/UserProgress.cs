namespace UpkeepAPI.Models;

public class UserProgress : BaseEntity
{
    public int CurrentLevel { get; set; }
    public int TotalXP { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastActivity { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
