using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Interfaces.Common;
using RPayroll.Infrastructure.Interfaces.Services;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.Repositories.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public UserService(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        EnsureAuthenticated();
        if (!IsAdmin())
        {
            throw new UnauthorizedAccessException("Only Admin can create users.");
        }

        var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId, includeInactive: true)
            ?? throw new InvalidOperationException("Role not found.");

        EnsureCanAssignRole(role);
        user.Role = role;
        user.CreatedDate = DateTime.UtcNow;
        user.Status = StatusCode.Accepted;
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return user;
    }

    public Task<User?> GetUserByIdAsync(int id)
    {
        EnsureAuthenticated();
        return _unitOfWork.Users.GetByIdAsync(id, includeInactive: true);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        EnsureAuthenticated();
        return await _unitOfWork.Users.GetAllAsync(includeInactive: true);
    }

    public async Task<User?> UpdateUserAsync(User user)
    {
        EnsureAuthenticated();
        EnsureCanManageUsers();

        var existingUser = await _unitOfWork.Users.GetByIdAsync(user.Id, includeInactive: true);
        if (existingUser == null)
        {
            return null;
        }

        var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId, includeInactive: true)
            ?? throw new InvalidOperationException("Role not found.");

        EnsureCanAssignRole(role);
        existingUser.Username = user.Username;
        if (!string.IsNullOrWhiteSpace(user.Password))
        {
            existingUser.Password = user.Password;
        }
        existingUser.RoleId = role.Id;
        existingUser.Role = role;
        existingUser.EmployeeId = user.EmployeeId;
        existingUser.Status = user.Status;
        existingUser.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(existingUser);
        await _unitOfWork.SaveChangesAsync();
        return existingUser;
    }

    public async Task<bool> DisableUserAsync(int id)
    {
        EnsureAuthenticated();
        EnsureCanManageUsers();

        var user = await _unitOfWork.Users.GetByIdAsync(id, includeInactive: true);
        if (user == null)
        {
            return false;
        }

        user.Status = StatusCode.Rejected;
        user.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private void EnsureCanManageUsers()
    {
        if (!IsAdmin() && !IsHr())
        {
            throw new UnauthorizedAccessException("Not allowed to manage users.");
        }
    }

    private void EnsureCanAssignRole(Role role)
    {
        if (IsManager() || IsEmployee())
        {
            throw new UnauthorizedAccessException("Not allowed to assign roles.");
        }

        if (IsHr() && string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("HR cannot assign Admin role.");
        }

        if (_currentUser.HierarchyLevel > role.HierarchyLevel)
        {
            throw new UnauthorizedAccessException("Cannot assign role higher than your own.");
        }
    }

    private bool IsAdmin() => string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);
    private bool IsHr() => string.Equals(_currentUser.Role, "HR", StringComparison.OrdinalIgnoreCase);
    private bool IsManager() => string.Equals(_currentUser.Role, "Manager", StringComparison.OrdinalIgnoreCase);
    private bool IsEmployee() => string.Equals(_currentUser.Role, "Employee", StringComparison.OrdinalIgnoreCase);

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }
    }
}
