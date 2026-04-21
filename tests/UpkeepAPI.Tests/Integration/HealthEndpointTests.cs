using System.Net;
using System.Net.Http.Json;
using UpkeepAPI.Tests.Fixtures;

namespace UpkeepAPI.Tests.Integration;

public class HealthEndpointTests : IntegrationTestBase
{
    public HealthEndpointTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task GetHealth_WithDatabaseAvailable_Returns200Healthy()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body!.Status.Should().Be("healthy");
        body.Database.Should().Be("connected");
    }

    private record HealthResponse(string Status, string Database, DateTime Timestamp);
}
