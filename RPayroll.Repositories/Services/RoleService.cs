using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Interfaces.Common;
using RPayroll.Infrastructure.Interfaces.Services;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.Repositories.Services;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public RoleService(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Role> CreateRoleAsync(Role role)
    {
        EnsureAuthenticated();
        EnsureAdmin();
        role.CreatedDate = DateTime.UtcNow;
        role.Status = StatusCode.Accepted;
        await _unitOfWork.Roles.AddAsync(role);
        await _unitOfWork.SaveChangesAsync();
        return role;
    }

    public Task<Role?> GetRoleByIdAsync(int id)
    {
        EnsureAuthenticated();
        return _unitOfWork.Roles.GetByIdAsync(id, includeInactive: true);
    }

    public async Task<IEnumerable<Role>> GetAllRolesAsync()
    {
        EnsureAuthenticated();
        return await _unitOfWork.Roles.GetAllAsync(includeInactive: true);
    }

    public async Task<Role?> UpdateRoleAsync(Role role)
    {
        EnsureAuthenticated();
        EnsureAdmin();

        var existingRole = await _unitOfWork.Roles.GetByIdAsync(role.Id, includeInactive: true);
        if (existingRole == null)
        {
            return null;
        }

        if (string.Equals(existingRole.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot update Admin role.");
        }

        existingRole.Name = role.Name;
        existingRole.HierarchyLevel = role.HierarchyLevel;
        existingRole.Status = role.Status;
        existingRole.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.Roles.UpdateAsync(existingRole);
        await _unitOfWork.SaveChangesAsync();
        return existingRole;
    }

    public async Task<bool> DeleteRoleAsync(int id)
    {
        EnsureAuthenticated();
        EnsureAdmin();
        var role = await _unitOfWork.Roles.GetByIdAsync(id, includeInactive: true);
        if (role == null)
        {
            return false;
        }

        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot delete Admin role.");
        }

        role.Status = StatusCode.Rejected;
        role.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.Roles.UpdateAsync(role);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private void EnsureAdmin()
    {
        if (!string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Admin role required.");
        }
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }
    }
}
