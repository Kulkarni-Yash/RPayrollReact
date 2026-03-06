using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int id, bool includeInactive = false);
    Task<Role?> GetByNameAsync(string name, bool includeInactive = false);
    Task<List<Role>> GetAllAsync(bool includeInactive = false);
    Task AddAsync(Role role);
    Task UpdateAsync(Role role);
}
