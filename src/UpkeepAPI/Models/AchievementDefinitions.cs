namespace UpkeepAPI.Models;

public record AchievementStats(
    int LongestStreak,
    int TotalLogsCompleted,
    int CurrentLevel,
    int TotalHabits
);

public enum AchievementKey
{
    FirstHabit,
    FirstLog,
    Logs10, Logs50, Logs100, Logs500,
    Streak3, Streak7, Streak14, Streak30, Streak100,
    Level5, Level10, Level25
}

public record AchievementDefinition(AchievementKey Key, string Title, string Description, string Icon);

public static class Achievements
{
    public static readonly IReadOnlyList<AchievementDefinition> All =
    [
        new(AchievementKey.FirstHabit, "Primeiro Hábito",  "Crie seu primeiro hábito",               "plus-circle"),
        new(AchievementKey.FirstLog,   "Primeiro Passo",   "Complete seu primeiro hábito",            "footprints"),
        new(AchievementKey.Logs10,     "Ganhando Ritmo",   "Complete 10 hábitos",                     "zap"),
        new(AchievementKey.Logs50,     "Consistente",      "Complete 50 hábitos",                     "trending-up"),
        new(AchievementKey.Logs100,    "Centenário",       "Complete 100 hábitos",                    "award"),
        new(AchievementKey.Logs500,    "Dedicado",         "Complete 500 hábitos",                    "star"),
        new(AchievementKey.Streak3,    "Começando Bem",    "Mantenha uma sequência de 3 dias",        "flame"),
        new(AchievementKey.Streak7,    "Uma Semana",       "Mantenha uma sequência de 7 dias",        "calendar-check"),
        new(AchievementKey.Streak14,   "Quinzena",         "Mantenha uma sequência de 14 dias",       "calendar-range"),
        new(AchievementKey.Streak30,   "Mês Completo",     "Mantenha uma sequência de 30 dias",       "trophy"),
        new(AchievementKey.Streak100,  "Centurião",        "Mantenha uma sequência de 100 dias",      "crown"),
        new(AchievementKey.Level5,     "Experiente",       "Atinja o nível 5",                        "shield"),
        new(AchievementKey.Level10,    "Veterano",         "Atinja o nível 10",                       "shield-check"),
        new(AchievementKey.Level25,    "Mestre",           "Atinja o nível 25",                       "gem"),
    ];

    public static bool IsUnlocked(AchievementKey key, AchievementStats s) => key switch
    {
        AchievementKey.FirstHabit => s.TotalHabits >= 1,
        AchievementKey.FirstLog   => s.TotalLogsCompleted >= 1,
        AchievementKey.Logs10     => s.TotalLogsCompleted >= 10,
        AchievementKey.Logs50     => s.TotalLogsCompleted >= 50,
        AchievementKey.Logs100    => s.TotalLogsCompleted >= 100,
        AchievementKey.Logs500    => s.TotalLogsCompleted >= 500,
        AchievementKey.Streak3    => s.LongestStreak >= 3,
        AchievementKey.Streak7    => s.LongestStreak >= 7,
        AchievementKey.Streak14   => s.LongestStreak >= 14,
        AchievementKey.Streak30   => s.LongestStreak >= 30,
        AchievementKey.Streak100  => s.LongestStreak >= 100,
        AchievementKey.Level5     => s.CurrentLevel >= 5,
        AchievementKey.Level10    => s.CurrentLevel >= 10,
        AchievementKey.Level25    => s.CurrentLevel >= 25,
        _                         => false
    };
}
