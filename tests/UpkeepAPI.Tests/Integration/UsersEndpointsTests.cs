using System.Net;
using System.Net.Http.Json;
using UpkeepAPI.DTOs.Auth;
using UpkeepAPI.DTOs.User;
using UpkeepAPI.Tests.Fixtures;

namespace UpkeepAPI.Tests.Integration;

public class UsersEndpointsTests : IntegrationTestBase
{
    public UsersEndpointsTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsAuthenticatedUser()
    {
        var auth = await RegisterAndAuthenticateAsync(
            name: "Carlos", email: "carlos@example.com");

        var response = await Client.GetAsync("/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserDto>();
        body!.Id.Should().Be(auth.User.Id);
        body.Email.Should().Be("carlos@example.com");
        body.Name.Should().Be("Carlos");
    }

    [Fact]
    public async Task UpdateMe_WithValidData_Returns200AndPersistsChanges()
    {
        await RegisterAndAuthenticateAsync(email: "update@example.com");

        var response = await Client.PutAsJsonAsync("/users/me", new UpdateUserDto
        {
            Name = "Novo Nome",
            Email = "novo@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserDto>();
        body!.Name.Should().Be("Novo Nome");
        body.Email.Should().Be("novo@example.com");

        var getMe = await Client.GetFromJsonAsync<UserDto>("/users/me");
        getMe!.Email.Should().Be("novo@example.com");
    }

    [Fact]
    public async Task UpdateMe_WithEmailTakenByAnotherUser_Returns409()
    {
        await Client.PostAsJsonAsync("/auth/register", new RegisterRequestDto
        {
            Name = "Outro", Email = "taken@example.com", Password = "password123"
        });
        await RegisterAndAuthenticateAsync(email: "mine@example.com");

        var response = await Client.PutAsJsonAsync("/users/me", new UpdateUserDto
        {
            Name = "Any", Email = "taken@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateMe_KeepingOwnEmail_Returns200()
    {
        await RegisterAndAuthenticateAsync(name: "Old", email: "same@example.com");

        var response = await Client.PutAsJsonAsync("/users/me", new UpdateUserDto
        {
            Name = "New", Email = "same@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateMe_WithInvalidEmail_Returns400()
    {
        await RegisterAndAuthenticateAsync();

        var response = await Client.PutAsJsonAsync("/users/me", new UpdateUserDto
        {
            Name = "Valid", Email = "not-an-email"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_Returns204AndPasswordChanges()
    {
        await RegisterAndAuthenticateAsync(
            email: "pwd@example.com", password: "old-password");

        var response = await Client.PatchAsJsonAsync("/users/me/password", new ChangePasswordDto
        {
            CurrentPassword = "old-password",
            NewPassword = "new-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var loginOld = await Client.PostAsJsonAsync("/auth/login", new LoginRequestDto
        {
            Email = "pwd@example.com", Password = "old-password"
        });
        loginOld.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var loginNew = await Client.PostAsJsonAsync("/auth/login", new LoginRequestDto
        {
            Email = "pwd@example.com", Password = "new-password"
        });
        loginNew.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns400()
    {
        await RegisterAndAuthenticateAsync(password: "real-password");

        var response = await Client.PatchAsJsonAsync("/users/me/password", new ChangePasswordDto
        {
            CurrentPassword = "wrong-password",
            NewPassword = "new-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithShortNewPassword_Returns400()
    {
        await RegisterAndAuthenticateAsync(password: "password123");

        var response = await Client.PatchAsJsonAsync("/users/me/password", new ChangePasswordDto
        {
            CurrentPassword = "password123",
            NewPassword = "123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteMe_Returns204AndSubsequentRequestsAre404()
    {
        await RegisterAndAuthenticateAsync(email: "delete@example.com");

        var response = await Client.DeleteAsync("/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await Client.GetAsync("/users/me");
        after.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
