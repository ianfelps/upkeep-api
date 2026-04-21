using Microsoft.EntityFrameworkCore;
using UpkeepAPI.Data;
using UpkeepAPI.DTOs.User;
using UpkeepAPI.Mappers;
using UpkeepAPI.Services.Interfaces;

namespace UpkeepAPI.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");

        return user.ToDto();
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");

        var emailTaken = await _context.Users
            .AnyAsync(u => u.Email == dto.Email.ToLower() && u.Id != id);

        if (emailTaken)
            throw new InvalidOperationException("E-mail já está em uso por outro usuário.");

        user.Name = dto.Name.Trim();
        user.Email = dto.Email.ToLower().Trim();

        await _context.SaveChangesAsync();

        return user.ToDto();
    }

    public async Task ChangePasswordAsync(Guid id, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Senha atual incorreta.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }
}
