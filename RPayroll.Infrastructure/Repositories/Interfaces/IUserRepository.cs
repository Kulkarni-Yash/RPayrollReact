using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, bool includeInactive = false);
    Task<User?> GetByUsernameAsync(string username, bool includeInactive = false);
    Task<User?> GetByTokenAsync(string token);
    Task<List<User>> GetAllAsync(bool includeInactive = false);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
