using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Repositories.Interfaces;

namespace RPayroll.Repositories.Repositories.Implementations;

public class FormLayoutRepository : IFormLayoutRepository
{
    public Task<List<FormLayoutDefinition>> GetAllAsync(bool includeInactive = true)
    {
        var query = InMemoryDataStore.FormLayouts.AsEnumerable();
        if (!includeInactive)
        {
            query = query.Where(item => item.Status == StatusCode.Accepted);
        }
        return Task.FromResult(query.OrderBy(item => item.Name).ToList());
    }

    public Task<FormLayoutDefinition?> GetByKeyAsync(string key, bool includeInactive = true)
    {
        var item = InMemoryDataStore.FormLayouts.FirstOrDefault(layout =>
            string.Equals(layout.Key, key, StringComparison.OrdinalIgnoreCase) &&
            (includeInactive || layout.Status == StatusCode.Accepted));
        return Task.FromResult(item);
    }

    public Task<FormLayoutDefinition?> GetByMenuKeyAsync(string menuKey, bool includeInactive = true)
    {
        var item = InMemoryDataStore.FormLayouts.FirstOrDefault(layout =>
            string.Equals(layout.MenuKey, menuKey, StringComparison.OrdinalIgnoreCase) &&
            (includeInactive || layout.Status == StatusCode.Accepted));
        return Task.FromResult(item);
    }

    public Task<FormLayoutDefinition?> GetByModelKeyAsync(string modelKey, bool includeInactive = true)
    {
        var item = InMemoryDataStore.FormLayouts.FirstOrDefault(layout =>
            string.Equals(layout.ModelKey, modelKey, StringComparison.OrdinalIgnoreCase) &&
            (includeInactive || layout.Status == StatusCode.Accepted));
        return Task.FromResult(item);
    }

    public Task AddAsync(FormLayoutDefinition item)
    {
        item.Id = InMemoryDataStore.NextFormLayoutId();
        InMemoryDataStore.FormLayouts.Add(item);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FormLayoutDefinition item)
    {
        var index = InMemoryDataStore.FormLayouts.FindIndex(layout => layout.Id == item.Id);
        if (index >= 0)
        {
            InMemoryDataStore.FormLayouts[index] = item;
        }
        return Task.CompletedTask;
    }
}
