using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UpkeepAPI.DTOs.Habit;
using UpkeepAPI.DTOs.HabitLog;
using UpkeepAPI.DTOs.UserProgress;
using UpkeepAPI.Models;
using UpkeepAPI.Tests.Fixtures;

namespace UpkeepAPI.Tests.Integration;

public class UserProgressEndpointsTests : IntegrationTestBase
{
    public UserProgressEndpointsTests(ApiFactory factory) : base(factory) { }

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

    [Fact]
    public async Task GetProgress_WithoutToken_Returns401()
    {
        Client.DefaultRequestHeaders.Authorization = null;
        var response = await Client.GetAsync("/users/me/progress");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProgress_NewUser_ReturnsZeroedStats()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.GetAsync("/users/me/progress");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserProgressDto>();
        body!.CurrentLevel.Should().Be(1);
        body.TotalXP.Should().Be(0);
        body.CurrentStreak.Should().Be(0);
        body.LongestStreak.Should().Be(0);
        body.LastActivity.Should().BeNull();
        body.TotalHabitsActive.Should().Be(0);
        body.TotalLogsCompleted.Should().Be(0);
        body.CompletionRateLast7Days.Should().Be(0);
        body.CompletionRateLast30Days.Should().Be(0);
    }

    [Fact]
    public async Task GetProgress_WithCompletedLogs_ComputesXPAndLevel()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await PostLogAsync(habit.Id, today, HabitStatus.Completed, xp: 50);
        await PostLogAsync(habit.Id, today.AddDays(-1), HabitStatus.Completed, xp: 50);

        var response = await Client.GetAsync("/users/me/progress");
        var body = await response.Content.ReadFromJsonAsync<UserProgressDto>();

        body!.TotalXP.Should().Be(100);
        body.CurrentLevel.Should().Be(2);
        body.TotalLogsCompleted.Should().Be(2);
    }

    [Fact]
    public async Task GetProgress_ConsecutiveDays_ReturnsCorrectStreaks()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await PostLogAsync(habit.Id, today);
        await PostLogAsync(habit.Id, today.AddDays(-1));

        var response = await Client.GetAsync("/users/me/progress");
        var body = await response.Content.ReadFromJsonAsync<UserProgressDto>();

        body!.CurrentStreak.Should().Be(2);
        body.LongestStreak.Should().Be(2);
    }

    [Fact]
    public async Task GetProgress_BrokenStreak_CurrentStreakIsZero()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await PostLogAsync(habit.Id, today.AddDays(-3));
        await PostLogAsync(habit.Id, today.AddDays(-2));

        var response = await Client.GetAsync("/users/me/progress");
        var body = await response.Content.ReadFromJsonAsync<UserProgressDto>();

        body!.CurrentStreak.Should().Be(0);
        body.LongestStreak.Should().Be(2);
    }

    [Fact]
    public async Task GetProgress_CompletionRate7Days_ReturnsCorrectRatio()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await PostLogAsync(habit.Id, today, HabitStatus.Completed);
        await PostLogAsync(habit.Id, today.AddDays(-1), HabitStatus.Completed);
        await PostLogAsync(habit.Id, today.AddDays(-2), HabitStatus.Completed);
        await PostLogAsync(habit.Id, today.AddDays(-3), HabitStatus.Missed);

        var response = await Client.GetAsync("/users/me/progress");
        var body = await response.Content.ReadFromJsonAsync<UserProgressDto>();

        body!.CompletionRateLast7Days.Should().Be(0.75m);
    }

    [Fact]
    public async Task GetProgress_LastActivity_IsMidnightUTC()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await PostLogAsync(habit.Id, today, HabitStatus.Completed);

        var response = await Client.GetAsync("/users/me/progress");
        var body = await response.Content.ReadFromJsonAsync<UserProgressDto>();

        body!.LastActivity.Should().NotBeNull();
        body.LastActivity!.Value.TimeOfDay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetProgress_OnlyCountsActiveHabits()
    {
        await RegisterAndAuthenticateAsync();
        var habit1 = await CreateHabitAsync("Hábito Ativo");
        var habit2 = await CreateHabitAsync("Hábito Inativo");

        await Client.PutAsJsonAsync($"/habits/{habit2.Id}", new UpdateHabitDto
        {
            Title = habit2.Title,
            Description = habit2.Description,
            LucideIcon = habit2.LucideIcon,
            Color = habit2.Color,
            FrequencyType = HabitFrequencyType.Daily,
            TargetValue = habit2.TargetValue,
            IsActive = false
        });

        var response = await Client.GetAsync("/users/me/progress");
        var body = await response.Content.ReadFromJsonAsync<UserProgressDto>();

        body!.TotalHabitsActive.Should().Be(1);
    }

    [Fact]
    public async Task GetProgress_IsIdempotent_CalledTwiceReturnsSameShape()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await PostLogAsync(habit.Id, today, HabitStatus.Completed, xp: 20);

        var r1 = await Client.GetAsync("/users/me/progress");
        var r2 = await Client.GetAsync("/users/me/progress");

        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        r2.StatusCode.Should().Be(HttpStatusCode.OK);

        var b1 = await r1.Content.ReadFromJsonAsync<UserProgressDto>();
        var b2 = await r2.Content.ReadFromJsonAsync<UserProgressDto>();

        b1!.TotalXP.Should().Be(b2!.TotalXP);
        b1.CurrentLevel.Should().Be(b2.CurrentLevel);
        b1.CurrentStreak.Should().Be(b2.CurrentStreak);
        b1.TotalLogsCompleted.Should().Be(b2.TotalLogsCompleted);
    }
}
