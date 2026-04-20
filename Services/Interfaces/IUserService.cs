using UpkeepAPI.DTOs.User;

namespace UpkeepAPI.Services.Interfaces;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(Guid id);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto);
    Task ChangePasswordAsync(Guid id, ChangePasswordDto dto);
    Task DeleteAsync(Guid id);
}
