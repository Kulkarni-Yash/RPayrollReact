using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Interfaces.Services;

public interface IRoleService
{
    Task<Role> CreateRoleAsync(Role role);
    Task<Role?> GetRoleByIdAsync(int id);
    Task<IEnumerable<Role>> GetAllRolesAsync();
    Task<Role?> UpdateRoleAsync(Role role);
    Task<bool> DeleteRoleAsync(int id);
}
