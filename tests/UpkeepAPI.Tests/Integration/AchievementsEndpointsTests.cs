using System.Net;
using System.Net.Http.Json;
using UpkeepAPI.DTOs.Habit;
using UpkeepAPI.DTOs.HabitLog;
using UpkeepAPI.DTOs.UserProgress;
using UpkeepAPI.Models;
using UpkeepAPI.Tests.Fixtures;

namespace UpkeepAPI.Tests.Integration;

public class AchievementsEndpointsTests : IntegrationTestBase
{
    public AchievementsEndpointsTests(ApiFactory factory) : base(factory) { }

    private async Task<HabitDto> CreateHabitAsync(string title = "Hábito Teste")
    {
        var dto = new CreateHabitDto
        {
            Title = title,
            Description = "Descrição",
            LucideIcon = "star",
            Color = "#2563EB",
            FrequencyType = HabitFrequencyType.Daily,
            TargetValue = 1
        };
        var response = await Client.PostAsJsonAsync("/habits", dto);
        return (await response.Content.ReadFromJsonAsync<HabitDto>())!;
    }

    private async Task PostLogAsync(Guid habitId, DateOnly date, HabitStatus status = HabitStatus.Completed, int xp = 10)
    {
        var dto = new CreateHabitLogDto { TargetDate = date, Status = status, EarnedXP = xp };
        await Client.PostAsJsonAsync($"/habits/{habitId}/logs", dto);
    }

    private async Task TriggerProgressAsync() =>
        await Client.GetAsync("/users/me/progress");

    [Fact]
    public async Task GetAchievements_WithoutToken_Returns401()
    {
        Client.DefaultRequestHeaders.Authorization = null;
        var response = await Client.GetAsync("/users/me/achievements");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAchievements_NewUser_ReturnsAllLockedWithCorrectCount()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.GetAsync("/users/me/achievements");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<AchievementDto>>();
        body!.Count.Should().Be(14);
        body.Should().OnlyContain(a => !a.IsUnlocked);
        body.Should().OnlyContain(a => a.UnlockedAt == null);
    }

    [Fact]
    public async Task GetAchievements_AfterFirstHabit_UnlocksFirstHabit()
    {
        await RegisterAndAuthenticateAsync();
        await CreateHabitAsync();
        await TriggerProgressAsync();

        var response = await Client.GetAsync("/users/me/achievements");
        var body = await response.Content.ReadFromJsonAsync<List<AchievementDto>>();

        var firstHabit = body!.Single(a => a.Key == "FirstHabit");
        firstHabit.IsUnlocked.Should().BeTrue();
    }

    [Fact]
    public async Task GetAchievements_AfterFirstLog_UnlocksFirstLog()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await PostLogAsync(habit.Id, today, HabitStatus.Completed);
        await TriggerProgressAsync();

        var response = await Client.GetAsync("/users/me/achievements");
        var body = await response.Content.ReadFromJsonAsync<List<AchievementDto>>();

        body!.Single(a => a.Key == "FirstLog").IsUnlocked.Should().BeTrue();
    }

    [Fact]
    public async Task GetAchievements_10Logs_UnlocksLogs10()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (int i = 0; i < 10; i++)
            await PostLogAsync(habit.Id, today.AddDays(-i), HabitStatus.Completed);

        await TriggerProgressAsync();

        var response = await Client.GetAsync("/users/me/achievements");
        var body = await response.Content.ReadFromJsonAsync<List<AchievementDto>>();

        body!.Single(a => a.Key == "Logs10").IsUnlocked.Should().BeTrue();
        body.Single(a => a.Key == "Logs50").IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public async Task GetAchievements_Streak7_UnlocksStreak3AndStreak7()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (int i = 0; i < 7; i++)
            await PostLogAsync(habit.Id, today.AddDays(-i), HabitStatus.Completed);

        await TriggerProgressAsync();

        var response = await Client.GetAsync("/users/me/achievements");
        var body = await response.Content.ReadFromJsonAsync<List<AchievementDto>>();

        body!.Single(a => a.Key == "Streak3").IsUnlocked.Should().BeTrue();
        body.Single(a => a.Key == "Streak7").IsUnlocked.Should().BeTrue();
        body.Single(a => a.Key == "Streak14").IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public async Task GetAchievements_UnlockedAt_IsSet()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await PostLogAsync(habit.Id, today, HabitStatus.Completed);
        await TriggerProgressAsync();

        var response = await Client.GetAsync("/users/me/achievements");
        var body = await response.Content.ReadFromJsonAsync<List<AchievementDto>>();

        var firstLog = body!.Single(a => a.Key == "FirstLog");
        firstLog.IsUnlocked.Should().BeTrue();
        firstLog.UnlockedAt.Should().NotBeNull();
        firstLog.UnlockedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task GetAchievements_IsIdempotent_NoDuplicates()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await PostLogAsync(habit.Id, today, HabitStatus.Completed);

        await TriggerProgressAsync();
        await TriggerProgressAsync();

        var response = await Client.GetAsync("/users/me/achievements");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<AchievementDto>>();

        body!.Count.Should().Be(14);
        body.Count(a => a.Key == "FirstLog").Should().Be(1);
        body.Single(a => a.Key == "FirstLog").IsUnlocked.Should().BeTrue();
    }
}
