using UpkeepAPI.DTOs.User;
using UpkeepAPI.Models;

namespace UpkeepAPI.Mappers;

public static class UserMapper
{
    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}
