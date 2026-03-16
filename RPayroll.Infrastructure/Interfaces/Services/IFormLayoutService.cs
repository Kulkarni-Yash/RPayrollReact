using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Interfaces.Services;

public interface IFormLayoutService
{
    Task<FormLayoutDefinition> SaveAsync(FormLayoutDefinition formLayout, string saveMode = "update", string? targetLayoutKey = null);
    Task<FormLayoutDefinition?> GetByKeyAsync(string key);
    Task<FormLayoutDefinition?> GetByMenuKeyAsync(string menuKey);
    Task<FormLayoutDefinition?> GetByModelKeyAsync(string modelKey);
    Task<IEnumerable<FormLayoutDefinition>> GetAllAsync();
}
