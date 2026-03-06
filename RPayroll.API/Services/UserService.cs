using RPayroll.Domain.DTOs.User;
using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Domain.Interfaces.Services;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.API.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public UserService(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        EnsureAuthenticated();
        if (!IsAdmin())
        {
            throw new UnauthorizedAccessException("Only Admin can create users.");
        }

        var role = await _unitOfWork.Roles.GetByIdAsync(dto.RoleId, includeInactive: true)
                   ?? throw new InvalidOperationException("Role not found.");

        EnsureCanAssignRole(role);

        var user = new User
        {
            Username = dto.Username,
            Password = dto.Password,
            RoleId = role.Id,
            Role = role,
            EmployeeId = dto.EmployeeId,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return MapUser(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        EnsureAuthenticated();
        var user = await _unitOfWork.Users.GetByIdAsync(id, includeInactive: true);
        return user == null ? null : MapUser(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        EnsureAuthenticated();
        var users = await _unitOfWork.Users.GetAllAsync(includeInactive: true);
        return users.Select(MapUser).ToList();
    }

    public async Task<UserDto?> UpdateUserAsync(UpdateUserDto dto)
    {
        EnsureAuthenticated();
        EnsureCanManageUsers();

        var user = await _unitOfWork.Users.GetByIdAsync(dto.Id, includeInactive: true);
        if (user == null)
        {
            return null;
        }

        var role = await _unitOfWork.Roles.GetByIdAsync(dto.RoleId, includeInactive: true)
                   ?? throw new InvalidOperationException("Role not found.");

        EnsureCanAssignRole(role);

        user.Username = dto.Username;
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.Password = dto.Password;
        }
        user.RoleId = role.Id;
        user.Role = role;
        user.EmployeeId = dto.EmployeeId;
        user.Status = (StatusCode)dto.Status;
        user.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return MapUser(user);
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

        if (_currentUser.HierarchyLevel <= role.HierarchyLevel)
        {
            return;
        }

        throw new UnauthorizedAccessException("Cannot assign role higher than your own.");
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

    private static UserDto MapUser(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name ?? string.Empty,
            EmployeeId = user.EmployeeId,
            Status = (int)user.Status
        };
    }
}
