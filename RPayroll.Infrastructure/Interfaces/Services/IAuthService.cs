using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Interfaces.Services;

public interface IAuthService
{
    Task<User?> LoginAsync(string username, string password);
    Task LogoutAsync(int userId);
}
