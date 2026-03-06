using RPayroll.Domain.DTOs.Role;
using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Domain.Interfaces.Services;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.API.Services;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public RoleService(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleDto dto)
    {
        EnsureAuthenticated();
        EnsureAdmin();

        var role = new Role
        {
            Name = dto.Name,
            HierarchyLevel = dto.HierarchyLevel,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };

        await _unitOfWork.Roles.AddAsync(role);
        await _unitOfWork.SaveChangesAsync();
        return MapRole(role);
    }

    public async Task<RoleDto?> GetRoleByIdAsync(int id)
    {
        EnsureAuthenticated();
        var role = await _unitOfWork.Roles.GetByIdAsync(id, includeInactive: true);
        return role == null ? null : MapRole(role);
    }

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        EnsureAuthenticated();
        var roles = await _unitOfWork.Roles.GetAllAsync(includeInactive: true);
        return roles.Select(MapRole).ToList();
    }

    public async Task<RoleDto?> UpdateRoleAsync(UpdateRoleDto dto)
    {
        EnsureAuthenticated();
        EnsureAdmin();

        var role = await _unitOfWork.Roles.GetByIdAsync(dto.Id, includeInactive: true);
        if (role == null)
        {
            return null;
        }

        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot update Admin role.");
        }

        role.Name = dto.Name;
        role.HierarchyLevel = dto.HierarchyLevel;
        role.Status = (StatusCode)dto.Status;
        role.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Roles.UpdateAsync(role);
        await _unitOfWork.SaveChangesAsync();
        return MapRole(role);
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

    private static RoleDto MapRole(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            HierarchyLevel = role.HierarchyLevel,
            Status = (int)role.Status
        };
    }
}
