using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UpkeepAPI.Data;
using UpkeepAPI.DTOs.Auth;
using UpkeepAPI.Mappers;
using UpkeepAPI.Models;
using UpkeepAPI.Services.Interfaces;

namespace UpkeepAPI.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IUserProgressService _userProgressService;

    public AuthService(AppDbContext context, IConfiguration configuration, IUserProgressService userProgressService)
    {
        _context = context;
        _configuration = configuration;
        _userProgressService = userProgressService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email.ToLower());
        if (emailExists)
            throw new InvalidOperationException("E-mail já cadastrado.");

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = dto.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _userProgressService.SeedAsync(user.Id);

        return await BuildAuthResponseAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("E-mail ou senha inválidos.");

        return await BuildAuthResponseAsync(user);
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);
        var stored = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (stored is null || stored.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token inválido ou expirado.");

        if (stored.RevokedAt is not null)
        {
            if (stored.ReplacedByTokenId is not null)
                await RevokeFamilyAsync(stored.FamilyId);

            throw new UnauthorizedAccessException("Refresh token inválido ou expirado.");
        }

        var now = DateTime.UtcNow;
        var (newToken, newExpiresAt, newId) = await IssueRefreshTokenAsync(stored.UserId, stored.FamilyId);

        stored.RevokedAt = now;
        stored.ReplacedByTokenId = newId;
        await _context.SaveChangesAsync();

        var (accessToken, accessExpiresAt) = GenerateAccessToken(stored.User);

        return new AuthResponseDto
        {
            Token = accessToken,
            TokenExpiresAt = accessExpiresAt,
            RefreshToken = newToken,
            RefreshTokenExpiresAt = newExpiresAt,
            User = stored.User.ToDto()
        };
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        var active = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        if (active.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var token in active)
            token.RevokedAt = now;

        await _context.SaveChangesAsync();
    }

    private async Task RevokeFamilyAsync(Guid familyId)
    {
        var active = await _context.RefreshTokens
            .Where(rt => rt.FamilyId == familyId && rt.RevokedAt == null)
            .ToListAsync();

        if (active.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var token in active)
            token.RevokedAt = now;

        await _context.SaveChangesAsync();
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);
        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (stored is null || stored.RevokedAt is not null)
            return;

        stored.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(User user)
    {
        var (token, tokenExpiresAt) = GenerateAccessToken(user);
        var (refreshToken, refreshExpiresAt, _) = await IssueRefreshTokenAsync(user.Id, Guid.NewGuid());

        return new AuthResponseDto
        {
            Token = token,
            TokenExpiresAt = tokenExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = user.ToDto()
        };
    }

    private (string Token, DateTime ExpiresAt) GenerateAccessToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiration = DateTime.UtcNow.AddHours(double.Parse(jwtSettings["ExpirationInHours"]!));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiration);
    }

    private async Task<(string Token, DateTime ExpiresAt, Guid Id)> IssueRefreshTokenAsync(Guid userId, Guid familyId)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var days = double.Parse(jwtSettings["RefreshExpirationInDays"] ?? "60");
        var expiresAt = DateTime.UtcNow.AddDays(days);

        var raw = GenerateRefreshTokenString();
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(raw),
            ExpiresAt = expiresAt,
            FamilyId = familyId
        };
        _context.RefreshTokens.Add(entity);
        await _context.SaveChangesAsync();

        return (raw, expiresAt, entity.Id);
    }

    private static string GenerateRefreshTokenString()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
