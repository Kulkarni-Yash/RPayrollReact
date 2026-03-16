using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Interfaces.Common;
using RPayroll.Infrastructure.Interfaces.Services;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.Repositories.Services;

public class MenuService : IMenuService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public MenuService(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<AppMenuItem>> GetVisibleMenuAsync()
    {
        EnsureAuthenticated();
        var items = await _unitOfWork.MenuItems.GetActiveAsync();
        return items.Where(IsVisibleToCurrentUser);
    }

    public async Task<IEnumerable<AppMenuItem>> GetAllMenuItemsAsync()
    {
        EnsureAdmin();
        return await _unitOfWork.MenuItems.GetAllAsync(includeInactive: true);
    }

    public async Task<AppMenuItem?> SetActiveAsync(string key, bool isActive)
    {
        EnsureAdmin();
        var item = await _unitOfWork.MenuItems.GetByKeyAsync(key, includeInactive: true);
        if (item == null)
        {
            return null;
        }

        if (item.IsSystem && !isActive)
        {
            throw new InvalidOperationException("System menu items cannot be deactivated.");
        }

        item.Status = isActive ? StatusCode.Accepted : StatusCode.Pending;
        item.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.MenuItems.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return item;
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }
    }

    private void EnsureAdmin()
    {
        if (!string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Admin role required.");
        }
    }

    private bool IsVisibleToCurrentUser(AppMenuItem item)
    {
        return item.VisibilityScope switch
        {
            VisibilityScope.AllUsers => true,
            VisibilityScope.RoleBased => _currentUser.HierarchyLevel <= (item.MaxHierarchyLevelAllowed ?? int.MaxValue),
            VisibilityScope.OwnerOnly => item.OwnerUserId == _currentUser.UserId,
            _ => false
        };
    }
}
