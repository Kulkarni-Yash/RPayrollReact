using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Repositories.Interfaces;

namespace RPayroll.Repositories.Repositories.Implementations;

public class MenuItemRepository : IMenuItemRepository
{
    public Task<List<AppMenuItem>> GetAllAsync(bool includeInactive = true)
    {
        var query = InMemoryDataStore.MenuItems.AsEnumerable();
        if (!includeInactive)
        {
            query = query.Where(item => item.Status == StatusCode.Accepted);
        }

        return Task.FromResult(query.OrderBy(item => item.Order).ThenBy(item => item.Label).ToList());
    }

    public Task<List<AppMenuItem>> GetActiveAsync()
    {
        var items = InMemoryDataStore.MenuItems
            .Where(item => item.Status == StatusCode.Accepted)
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Label)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<AppMenuItem?> GetByKeyAsync(string key, bool includeInactive = true)
    {
        var item = InMemoryDataStore.MenuItems.FirstOrDefault(menu =>
            string.Equals(menu.Key, key, StringComparison.OrdinalIgnoreCase) &&
            (includeInactive || menu.Status == StatusCode.Accepted));
        return Task.FromResult(item);
    }

    public Task AddAsync(AppMenuItem item)
    {
        item.Id = InMemoryDataStore.NextMenuItemId();
        InMemoryDataStore.MenuItems.Add(item);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AppMenuItem item)
    {
        var index = InMemoryDataStore.MenuItems.FindIndex(menu => menu.Id == item.Id);
        if (index >= 0)
        {
            InMemoryDataStore.MenuItems[index] = item;
        }
        return Task.CompletedTask;
    }
}
