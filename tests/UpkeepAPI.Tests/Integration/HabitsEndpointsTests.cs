using System.Net;
using System.Net.Http.Json;
using UpkeepAPI.DTOs.Habit;
using UpkeepAPI.Models;
using UpkeepAPI.Tests.Fixtures;

namespace UpkeepAPI.Tests.Integration;

public class HabitsEndpointsTests : IntegrationTestBase
{
    public HabitsEndpointsTests(ApiFactory factory) : base(factory) { }

    private static CreateHabitDto ValidCreateDto(
        string title = "Meditação",
        string color = "#2563EB",
        HabitFrequencyType frequencyType = HabitFrequencyType.Daily,
        int targetValue = 1,
        Guid[]? routineEventIds = null) => new()
        {
            Title = title,
            Description = "Sessão matinal",
            LucideIcon = "brain",
            Color = color,
            FrequencyType = frequencyType,
            TargetValue = targetValue,
            RoutineEventIds = routineEventIds
        };

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/habits");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithValidData_Returns201AndLocationHeader()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/habits", ValidCreateDto());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var body = await response.Content.ReadFromJsonAsync<HabitDto>();
        body!.Title.Should().Be("Meditação");
        body.Color.Should().Be("#2563EB");
        body.FrequencyType.Should().Be("Daily");
        body.IsActive.Should().BeTrue();
        body.Id.Should().NotBe(Guid.Empty);
        body.LinkedRoutineEventIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithoutTitle_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/habits", ValidCreateDto(title: ""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithTargetValueZero_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/habits", ValidCreateDto(targetValue: 0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithEmptyRoutineEventIds_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/habits", ValidCreateDto(routineEventIds: []));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyHabitsOfAuthenticatedUser()
    {
        await RegisterAndAuthenticateAsync(email: "a@example.com");
        await Client.PostAsJsonAsync("/habits", ValidCreateDto(title: "Hábito do A"));

        var tokenA = Client.DefaultRequestHeaders.Authorization;

        Client.DefaultRequestHeaders.Authorization = null;
        await RegisterAndAuthenticateAsync(name: "B", email: "b@example.com");
        await Client.PostAsJsonAsync("/habits", ValidCreateDto(title: "Hábito do B"));

        var since = Uri.EscapeDataString("2000-01-01T00:00:00Z");
        var listB = (await Client.GetFromJsonAsync<List<HabitDto>>($"/habits?updatedSince={since}"))!;
        listB.Should().HaveCount(1);
        listB[0].Title.Should().Be("Hábito do B");

        Client.DefaultRequestHeaders.Authorization = tokenA;
        var listA = (await Client.GetFromJsonAsync<List<HabitDto>>($"/habits?updatedSince={since}"))!;
        listA.Should().HaveCount(1);
        listA[0].Title.Should().Be("Hábito do A");
    }

    [Fact]
    public async Task GetAll_WithUpdatedSince_ReturnsOnlyNewerHabits()
    {
        await RegisterAndAuthenticateAsync();

        await Client.PostAsJsonAsync("/habits", ValidCreateDto(title: "Antigo"));

        await Task.Delay(50);
        var cutoff = DateTime.UtcNow;
        await Task.Delay(50);

        await Client.PostAsJsonAsync("/habits", ValidCreateDto(title: "Novo"));

        var isoCutoff = Uri.EscapeDataString(cutoff.ToString("O"));
        var list = (await Client.GetFromJsonAsync<List<HabitDto>>($"/habits?updatedSince={isoCutoff}"))!;

        list.Should().HaveCount(1);
        list[0].Title.Should().Be("Novo");
    }

    [Fact]
    public async Task GetById_OwnHabit_Returns200()
    {
        await RegisterAndAuthenticateAsync();

        var created = (await (await Client.PostAsJsonAsync("/habits", ValidCreateDto()))
            .Content.ReadFromJsonAsync<HabitDto>())!;

        var response = await Client.GetAsync($"/habits/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<HabitDto>();
        body!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetById_OtherUsersHabit_Returns404()
    {
        await RegisterAndAuthenticateAsync(email: "owner@example.com");
        var created = (await (await Client.PostAsJsonAsync("/habits", ValidCreateDto()))
            .Content.ReadFromJsonAsync<HabitDto>())!;

        Client.DefaultRequestHeaders.Authorization = null;
        await RegisterAndAuthenticateAsync(name: "Outro", email: "other@example.com");

        var response = await Client.GetAsync($"/habits/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.GetAsync($"/habits/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_OwnHabit_Returns200AndPersistsChanges()
    {
        await RegisterAndAuthenticateAsync();
        var created = (await (await Client.PostAsJsonAsync("/habits", ValidCreateDto()))
            .Content.ReadFromJsonAsync<HabitDto>())!;

        var update = new UpdateHabitDto
        {
            Title = "Leitura",
            Description = "30 minutos",
            LucideIcon = "book",
            Color = "#7C3AED",
            FrequencyType = HabitFrequencyType.Weekly,
            TargetValue = 3,
            IsActive = true
        };

        var response = await Client.PutAsJsonAsync($"/habits/{created.Id}", update);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<HabitDto>();
        body!.Title.Should().Be("Leitura");
        body.Color.Should().Be("#7C3AED");
        body.FrequencyType.Should().Be("Weekly");
        body.TargetValue.Should().Be(3);
    }

    [Fact]
    public async Task Update_OtherUsersHabit_Returns404()
    {
        await RegisterAndAuthenticateAsync(email: "owner@example.com");
        var created = (await (await Client.PostAsJsonAsync("/habits", ValidCreateDto()))
            .Content.ReadFromJsonAsync<HabitDto>())!;

        Client.DefaultRequestHeaders.Authorization = null;
        await RegisterAndAuthenticateAsync(name: "Outro", email: "other@example.com");

        var response = await Client.PutAsJsonAsync($"/habits/{created.Id}", new UpdateHabitDto
        {
            Title = "hack",
            Color = "#000000",
            FrequencyType = HabitFrequencyType.Daily,
            TargetValue = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_OwnHabit_Returns204AndSubsequentGetIs404()
    {
        await RegisterAndAuthenticateAsync();
        var created = (await (await Client.PostAsJsonAsync("/habits", ValidCreateDto()))
            .Content.ReadFromJsonAsync<HabitDto>())!;

        var response = await Client.DeleteAsync($"/habits/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await Client.GetAsync($"/habits/{created.Id}");
        after.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHeatmap_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/habits/heatmap");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHeatmap_WithNoLogs_ReturnsEmptyList()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.GetAsync("/habits/heatmap");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<List<HabitHeatmapEntryDto>>();
        body.Should().BeEmpty();
    }
}
