using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Repositories.Interfaces;

namespace RPayroll.Infrastructure.Repositories.Implementations;

public class RoleRepository : IRoleRepository
{
    public Task<Role?> GetByIdAsync(int id, bool includeInactive = false)
    {
        var role = InMemoryDataStore.Roles.FirstOrDefault(r =>
            r.Id == id && (includeInactive || r.Status != StatusCode.Rejected));
        return Task.FromResult(role);
    }

    public Task<Role?> GetByNameAsync(string name, bool includeInactive = false)
    {
        var role = InMemoryDataStore.Roles.FirstOrDefault(r =>
            string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase) &&
            (includeInactive || r.Status != StatusCode.Rejected));
        return Task.FromResult(role);
    }

    public Task<List<Role>> GetAllAsync(bool includeInactive = false)
    {
        var query = InMemoryDataStore.Roles.AsEnumerable();
        if (!includeInactive)
        {
            query = query.Where(r => r.Status != StatusCode.Rejected);
        }
        return Task.FromResult(query.ToList());
    }

    public Task AddAsync(Role role)
    {
        role.Id = InMemoryDataStore.NextRoleId();
        InMemoryDataStore.Roles.Add(role);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Role role)
    {
        var index = InMemoryDataStore.Roles.FindIndex(r => r.Id == role.Id);
        if (index >= 0)
        {
            InMemoryDataStore.Roles[index] = role;
        }
        return Task.CompletedTask;
    }
}
