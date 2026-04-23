using System.Net;
using System.Net.Http.Json;
using UpkeepAPI.DTOs.RoutineEvent;
using UpkeepAPI.Tests.Fixtures;

namespace UpkeepAPI.Tests.Integration;

public class RoutineEventsEndpointsTests : IntegrationTestBase
{
    public RoutineEventsEndpointsTests(ApiFactory factory) : base(factory) { }

    private static CreateRoutineEventDto ValidCreateDto(
        string title = "Meditação",
        string? description = "Sessão matinal",
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        int[]? daysOfWeek = null,
        string? color = null) => new()
        {
            Title = title,
            Description = description,
            StartTime = startTime ?? new TimeSpan(7, 0, 0),
            EndTime = endTime ?? new TimeSpan(7, 30, 0),
            DaysOfWeek = daysOfWeek ?? new[] { 1, 3, 5 },
            Color = color
        };

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/routine-events");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithValidData_Returns201AndLocationHeader()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/routine-events", ValidCreateDto());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var body = await response.Content.ReadFromJsonAsync<RoutineEventDto>();
        body!.Title.Should().Be("Meditação");
        body.DaysOfWeek.Should().BeEquivalentTo(new[] { 1, 3, 5 });
        body.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Create_WithColor_PersistsColor()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/routine-events",
            ValidCreateDto(color: "#2563EB"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<RoutineEventDto>();
        body!.Color.Should().Be("#2563EB");
    }

    [Fact]
    public async Task Create_WithoutColor_ReturnsNullColor()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/routine-events", ValidCreateDto());

        var body = await response.Content.ReadFromJsonAsync<RoutineEventDto>();
        body!.Color.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithoutTitle_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/routine-events", ValidCreateDto(title: ""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithEmptyDaysOfWeek_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/routine-events",
            ValidCreateDto(daysOfWeek: Array.Empty<int>()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithDayOutOfRange_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/routine-events",
            ValidCreateDto(daysOfWeek: new[] { 1, 9 }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithEndTimeBeforeStartTime_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/routine-events",
            ValidCreateDto(
                startTime: new TimeSpan(8, 0, 0),
                endTime: new TimeSpan(7, 0, 0)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyEventsOfAuthenticatedUser()
    {
        await RegisterAndAuthenticateAsync(email: "a@example.com");
        await Client.PostAsJsonAsync("/routine-events", ValidCreateDto(title: "Do usuário A"));

        var tokenA = Client.DefaultRequestHeaders.Authorization;

        Client.DefaultRequestHeaders.Authorization = null;
        await RegisterAndAuthenticateAsync(name: "B", email: "b@example.com");
        await Client.PostAsJsonAsync("/routine-events", ValidCreateDto(title: "Do usuário B"));

        var since = Uri.EscapeDataString("2000-01-01T00:00:00Z");
        var listB = (await Client.GetFromJsonAsync<List<RoutineEventDto>>($"/routine-events?updatedSince={since}"))!;
        listB.Should().HaveCount(1);
        listB[0].Title.Should().Be("Do usuário B");

        Client.DefaultRequestHeaders.Authorization = tokenA;
        var listA = (await Client.GetFromJsonAsync<List<RoutineEventDto>>($"/routine-events?updatedSince={since}"))!;
        listA.Should().HaveCount(1);
        listA[0].Title.Should().Be("Do usuário A");
    }

    [Fact]
    public async Task GetAll_WithUpdatedSince_ReturnsOnlyNewerEvents()
    {
        await RegisterAndAuthenticateAsync();

        var firstResp = await Client.PostAsJsonAsync("/routine-events",
            ValidCreateDto(title: "Antigo"));
        var first = (await firstResp.Content.ReadFromJsonAsync<RoutineEventDto>())!;

        await Task.Delay(50);
        var cutoff = DateTime.UtcNow;
        await Task.Delay(50);

        await Client.PostAsJsonAsync("/routine-events", ValidCreateDto(title: "Novo"));

        var isoCutoff = cutoff.ToString("O");
        var list = (await Client.GetFromJsonAsync<List<RoutineEventDto>>(
            $"/routine-events?updatedSince={Uri.EscapeDataString(isoCutoff)}"))!;

        list.Should().HaveCount(1);
        list[0].Title.Should().Be("Novo");
        list[0].Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task GetById_OwnEvent_Returns200()
    {
        await RegisterAndAuthenticateAsync();

        var created = (await (await Client.PostAsJsonAsync("/routine-events", ValidCreateDto()))
            .Content.ReadFromJsonAsync<RoutineEventDto>())!;

        var response = await Client.GetAsync($"/routine-events/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RoutineEventDto>();
        body!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetById_OtherUsersEvent_Returns404()
    {
        await RegisterAndAuthenticateAsync(email: "owner@example.com");
        var created = (await (await Client.PostAsJsonAsync("/routine-events", ValidCreateDto()))
            .Content.ReadFromJsonAsync<RoutineEventDto>())!;

        Client.DefaultRequestHeaders.Authorization = null;
        await RegisterAndAuthenticateAsync(name: "Outro", email: "other@example.com");

        var response = await Client.GetAsync($"/routine-events/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.GetAsync($"/routine-events/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_OwnEvent_Returns200AndPersistsChanges()
    {
        await RegisterAndAuthenticateAsync();
        var created = (await (await Client.PostAsJsonAsync("/routine-events", ValidCreateDto()))
            .Content.ReadFromJsonAsync<RoutineEventDto>())!;

        var update = new UpdateRoutineEventDto
        {
            Title = "Atualizado",
            Description = "Nova descrição",
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 0, 0),
            DaysOfWeek = new[] { 2, 4 },
            Color = "#7C3AED"
        };

        var response = await Client.PutAsJsonAsync($"/routine-events/{created.Id}", update);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<RoutineEventDto>();
        body!.Title.Should().Be("Atualizado");
        body.Color.Should().Be("#7C3AED");
        body.DaysOfWeek.Should().BeEquivalentTo(new[] { 2, 4 });
    }

    [Fact]
    public async Task Update_OtherUsersEvent_Returns404()
    {
        await RegisterAndAuthenticateAsync(email: "owner@example.com");
        var created = (await (await Client.PostAsJsonAsync("/routine-events", ValidCreateDto()))
            .Content.ReadFromJsonAsync<RoutineEventDto>())!;

        Client.DefaultRequestHeaders.Authorization = null;
        await RegisterAndAuthenticateAsync(name: "Outro", email: "other@example.com");

        var response = await Client.PutAsJsonAsync($"/routine-events/{created.Id}",
            new UpdateRoutineEventDto
            {
                Title = "hack",
                StartTime = new TimeSpan(7, 0, 0),
                DaysOfWeek = new[] { 1 }
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_OwnEvent_Returns204AndSubsequentGetIs404()
    {
        await RegisterAndAuthenticateAsync();
        var created = (await (await Client.PostAsJsonAsync("/routine-events", ValidCreateDto()))
            .Content.ReadFromJsonAsync<RoutineEventDto>())!;

        var response = await Client.DeleteAsync($"/routine-events/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await Client.GetAsync($"/routine-events/{created.Id}");
        after.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
