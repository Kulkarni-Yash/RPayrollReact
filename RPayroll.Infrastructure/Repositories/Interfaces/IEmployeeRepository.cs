using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Repositories.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int id, bool includeInactive = false);
    Task<List<Employee>> GetAllAsync(bool includeInactive = false);
    Task AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
}
