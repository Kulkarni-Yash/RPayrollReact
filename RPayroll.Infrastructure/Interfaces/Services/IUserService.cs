using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Interfaces.Services;

public interface IUserService
{
    Task<User> CreateUserAsync(User user);
    Task<User?> GetUserByIdAsync(int id);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> UpdateUserAsync(User user);
    Task<bool> DisableUserAsync(int id);
}
