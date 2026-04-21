using System.Net;
using System.Net.Http.Json;
using UpkeepAPI.DTOs.Auth;
using UpkeepAPI.Tests.Fixtures;

namespace UpkeepAPI.Tests.Integration;

public class AuthEndpointsTests : IntegrationTestBase
{
    public AuthEndpointsTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_WithValidData_Returns201WithTokenAndUser()
    {
        var response = await Client.PostAsJsonAsync("/auth/register", new RegisterRequestDto
        {
            Name = "Maria Silva",
            Email = "maria@example.com",
            Password = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.User.Email.Should().Be("maria@example.com");
        body.User.Name.Should().Be("Maria Silva");
        body.User.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Register_NormalizesEmailToLowercaseAndTrimsName()
    {
        var response = await Client.PostAsJsonAsync("/auth/register", new RegisterRequestDto
        {
            Name = "  João  ",
            Email = "JOAO@EXAMPLE.COM",
            Password = "password123"
        });

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body!.User.Email.Should().Be("joao@example.com");
        body.User.Name.Should().Be("João");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        await RegisterAndAuthenticateAsync(email: "dup@example.com");

        var response = await Client.PostAsJsonAsync("/auth/register", new RegisterRequestDto
        {
            Name = "Other",
            Email = "dup@example.com",
            Password = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("", "valid@example.com", "password123")]
    [InlineData("Name", "not-an-email", "password123")]
    [InlineData("Name", "valid@example.com", "123")]
    public async Task Register_WithInvalidData_Returns400(string name, string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/auth/register",
            new RegisterRequestDto { Name = name, Email = email, Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        await Client.PostAsJsonAsync("/auth/register", new RegisterRequestDto
        {
            Name = "User",
            Email = "login@example.com",
            Password = "password123"
        });

        var response = await Client.PostAsJsonAsync("/auth/login", new LoginRequestDto
        {
            Email = "login@example.com",
            Password = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.User.Email.Should().Be("login@example.com");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await Client.PostAsJsonAsync("/auth/register", new RegisterRequestDto
        {
            Name = "User",
            Email = "wrongpass@example.com",
            Password = "password123"
        });

        var response = await Client.PostAsJsonAsync("/auth/login", new LoginRequestDto
        {
            Email = "wrongpass@example.com",
            Password = "wrong-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/auth/login", new LoginRequestDto
        {
            Email = "nobody@example.com",
            Password = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
