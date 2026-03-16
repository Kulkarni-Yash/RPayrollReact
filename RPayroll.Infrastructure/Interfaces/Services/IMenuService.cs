using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Interfaces.Services;

public interface IMenuService
{
    Task<IEnumerable<AppMenuItem>> GetVisibleMenuAsync();
    Task<IEnumerable<AppMenuItem>> GetAllMenuItemsAsync();
    Task<AppMenuItem?> SetActiveAsync(string key, bool isActive);
}
