using System.Net;
using System.Net.Http.Json;
using UpkeepAPI.DTOs.Habit;
using UpkeepAPI.DTOs.HabitLog;
using UpkeepAPI.Models;
using UpkeepAPI.Tests.Fixtures;

namespace UpkeepAPI.Tests.Integration;

public class HabitLogsEndpointsTests : IntegrationTestBase
{
    public HabitLogsEndpointsTests(ApiFactory factory) : base(factory) { }

    private async Task<HabitDto> CreateHabitAsync(string title = "Meditação")
    {
        var dto = new CreateHabitDto
        {
            Title = title,
            Description = "Sessão matinal",
            Icon = "brain",
            Color = "#2563EB",
            FrequencyType = HabitFrequencyType.Daily,
            TargetValue = 1
        };
        var response = await Client.PostAsJsonAsync("/habits", dto);
        return (await response.Content.ReadFromJsonAsync<HabitDto>())!;
    }

    private static CreateHabitLogDto ValidLogDto(
        DateOnly? date = null,
        HabitStatus status = HabitStatus.Completed,
        string? notes = null) => new()
        {
            TargetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Status = status,
            Notes = notes,
            EarnedXP = 10
        };

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync($"/habits/{Guid.NewGuid()}/logs");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_NonExistentHabit_Returns404()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.GetAsync($"/habits/{Guid.NewGuid()}/logs");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithValidData_Returns201()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();

        var response = await Client.PostAsJsonAsync($"/habits/{habit.Id}/logs", ValidLogDto());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var body = await response.Content.ReadFromJsonAsync<HabitLogDto>();
        body!.Status.Should().Be("Completed");
        body.HabitId.Should().Be(habit.Id);
        body.CompletedAt.Should().NotBeNull();
        body.EarnedXP.Should().Be(10);
    }

    [Fact]
    public async Task Create_DuplicateDate_Returns400()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        await Client.PostAsJsonAsync($"/habits/{habit.Id}/logs", ValidLogDto(date: date));
        var response = await Client.PostAsJsonAsync($"/habits/{habit.Id}/logs", ValidLogDto(date: date));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_SkippedStatus_CompletedAtIsNull()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();

        var response = await Client.PostAsJsonAsync($"/habits/{habit.Id}/logs",
            ValidLogDto(status: HabitStatus.Skipped));

        var body = await response.Content.ReadFromJsonAsync<HabitLogDto>();
        body!.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Create_ForOtherUsersHabit_Returns404()
    {
        await RegisterAndAuthenticateAsync(email: "owner@example.com");
        var habit = await CreateHabitAsync();

        Client.DefaultRequestHeaders.Authorization = null;
        await RegisterAndAuthenticateAsync(name: "Outro", email: "other@example.com");

        var response = await Client.PostAsJsonAsync($"/habits/{habit.Id}/logs", ValidLogDto());
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_WithDateRange_ReturnsFilteredLogs()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();

        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        await Client.PostAsJsonAsync($"/habits/{habit.Id}/logs", ValidLogDto(date: yesterday));
        await Client.PostAsJsonAsync($"/habits/{habit.Id}/logs", ValidLogDto(date: today));
        await Client.PostAsJsonAsync($"/habits/{habit.Id}/logs", ValidLogDto(date: tomorrow));

        var from = Uri.EscapeDataString(yesterday.ToString("yyyy-MM-dd"));
        var to = Uri.EscapeDataString(today.ToString("yyyy-MM-dd"));
        var list = (await Client.GetFromJsonAsync<List<HabitLogDto>>($"/habits/{habit.Id}/logs?from={from}&to={to}"))!;

        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WithUpdatedSince_ReturnsOnlyNewerLogs()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();

        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        await Client.PostAsJsonAsync($"/habits/{habit.Id}/logs", ValidLogDto(date: yesterday));

        await Task.Delay(50);
        var cutoff = DateTime.UtcNow;
        await Task.Delay(50);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await Client.PostAsJsonAsync($"/habits/{habit.Id}/logs", ValidLogDto(date: today));

        var isoCutoff = Uri.EscapeDataString(cutoff.ToString("O"));
        var list = (await Client.GetFromJsonAsync<List<HabitLogDto>>($"/habits/{habit.Id}/logs?updatedSince={isoCutoff}"))!;

        list.Should().HaveCount(1);
        list[0].TargetDate.Should().Be(today);
    }

    [Fact]
    public async Task GetById_OwnLog_Returns200()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();

        var created = (await (await Client.PostAsJsonAsync($"/habits/{habit.Id}/logs", ValidLogDto()))
            .Content.ReadFromJsonAsync<HabitLogDto>())!;

        var response = await Client.GetAsync($"/habits/{habit.Id}/logs/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_OwnLog_Returns200AndChangesStatus()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();

        var created = (await (await Client.PostAsJsonAsync($"/habits/{habit.Id}/logs", ValidLogDto()))
            .Content.ReadFromJsonAsync<HabitLogDto>())!;

        created.Status.Should().Be("Completed");
        created.CompletedAt.Should().NotBeNull();

        var update = new UpdateHabitLogDto { Status = HabitStatus.Skipped, EarnedXP = 0 };
        var response = await Client.PutAsJsonAsync($"/habits/{habit.Id}/logs/{created.Id}", update);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<HabitLogDto>();
        body!.Status.Should().Be("Skipped");
        body.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Delete_OwnLog_Returns204AndSubsequentGetIs404()
    {
        await RegisterAndAuthenticateAsync();
        var habit = await CreateHabitAsync();

        var created = (await (await Client.PostAsJsonAsync($"/habits/{habit.Id}/logs", ValidLogDto()))
            .Content.ReadFromJsonAsync<HabitLogDto>())!;

        var response = await Client.DeleteAsync($"/habits/{habit.Id}/logs/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await Client.GetAsync($"/habits/{habit.Id}/logs/{created.Id}");
        after.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
