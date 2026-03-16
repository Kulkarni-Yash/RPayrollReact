using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Repositories.Interfaces;

public interface IMenuItemRepository
{
    Task<List<AppMenuItem>> GetAllAsync(bool includeInactive = true);
    Task<List<AppMenuItem>> GetActiveAsync();
    Task<AppMenuItem?> GetByKeyAsync(string key, bool includeInactive = true);
    Task AddAsync(AppMenuItem item);
    Task UpdateAsync(AppMenuItem item);
}
