using System.Net.Http.Headers;
using System.Net.Http.Json;
using UpkeepAPI.DTOs.Auth;

namespace UpkeepAPI.Tests.Fixtures;

[Collection("Api")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly ApiFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(ApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task<AuthResponseDto> RegisterAndAuthenticateAsync(
        string name = "Test User",
        string email = "test@example.com",
        string password = "password123")
    {
        var response = await Client.PostAsJsonAsync("/auth/register",
            new RegisterRequestDto { Name = name, Email = email, Password = password });

        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return auth;
    }
}

[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiFactory>;
