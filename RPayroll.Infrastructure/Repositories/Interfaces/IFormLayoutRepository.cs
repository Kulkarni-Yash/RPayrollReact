using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Repositories.Interfaces;

public interface IFormLayoutRepository
{
    Task<List<FormLayoutDefinition>> GetAllAsync(bool includeInactive = true);
    Task<FormLayoutDefinition?> GetByKeyAsync(string key, bool includeInactive = true);
    Task<FormLayoutDefinition?> GetByMenuKeyAsync(string menuKey, bool includeInactive = true);
    Task<FormLayoutDefinition?> GetByModelKeyAsync(string modelKey, bool includeInactive = true);
    Task AddAsync(FormLayoutDefinition item);
    Task UpdateAsync(FormLayoutDefinition item);
}
