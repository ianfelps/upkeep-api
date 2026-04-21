using UpkeepAPI.DTOs.User;

namespace UpkeepAPI.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}
