namespace UpkeepAPI.Models;

public class UserAchievement : BaseEntity
{
    public Guid UserId { get; set; }
    public AchievementKey Key { get; set; }
    public User User { get; set; } = null!;
}
