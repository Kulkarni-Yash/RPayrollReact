using RPayroll.Domain.DTOs.User;

namespace RPayroll.Domain.Interfaces.Services;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> UpdateUserAsync(UpdateUserDto dto);
    Task<bool> DisableUserAsync(int id);
}
