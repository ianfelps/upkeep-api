using System.Net;
using System.Net.Http.Json;
using UpkeepAPI.DTOs.Auth;
using UpkeepAPI.Tests.Fixtures;

namespace UpkeepAPI.Tests.Integration;

public class RefreshTokenEndpointsTests : IntegrationTestBase
{
    public RefreshTokenEndpointsTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_ReturnsAccessAndRefreshTokens()
    {
        var response = await Client.PostAsJsonAsync("/auth/register", new RegisterRequestDto
        {
            Name = "Ana", Email = "ana@example.com", Password = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        body.Token.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.TokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
        body.RefreshTokenExpiresAt.Should().BeAfter(body.TokenExpiresAt);
    }

    [Fact]
    public async Task Refresh_WithValidToken_Returns200AndRotatesTokens()
    {
        var auth = await RegisterAndAuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequestDto { RefreshToken = auth.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        body.Token.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBe(auth.RefreshToken);

        // antigo refresh token foi revogado
        var reuse = await Client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequestDto { RefreshToken = auth.RefreshToken });
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequestDto { RefreshToken = "nao-existe" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithEmptyToken_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequestDto { RefreshToken = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken_SubsequentRefreshReturns401()
    {
        var auth = await RegisterAndAuthenticateAsync();

        var logoutResp = await Client.PostAsJsonAsync("/auth/logout",
            new RefreshTokenRequestDto { RefreshToken = auth.RefreshToken });
        logoutResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshResp = await Client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequestDto { RefreshToken = auth.RefreshToken });
        refreshResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_UnknownToken_Returns204()
    {
        var response = await Client.PostAsJsonAsync("/auth/logout",
            new RefreshTokenRequestDto { RefreshToken = "token-qualquer" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
