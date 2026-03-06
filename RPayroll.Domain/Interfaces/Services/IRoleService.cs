using RPayroll.Domain.DTOs.Role;

namespace RPayroll.Domain.Interfaces.Services;

public interface IRoleService
{
    Task<RoleDto> CreateRoleAsync(CreateRoleDto dto);
    Task<RoleDto?> GetRoleByIdAsync(int id);
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    Task<RoleDto?> UpdateRoleAsync(UpdateRoleDto dto);
    Task<bool> DeleteRoleAsync(int id);
}
