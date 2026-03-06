using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Repositories.Interfaces;

namespace RPayroll.Infrastructure.Repositories.Implementations;

public class EmployeeRepository : IEmployeeRepository
{
    public Task<Employee?> GetByIdAsync(int id, bool includeInactive = false)
    {
        var employee = InMemoryDataStore.Employees.FirstOrDefault(e =>
            e.Id == id && (includeInactive || e.Status != StatusCode.Rejected));
        if (employee != null && !includeInactive)
        {
            employee.ContactPersons = employee.ContactPersons
                .Where(c => c.Status != StatusCode.Rejected)
                .ToList();
        }
        return Task.FromResult(employee);
    }

    public Task<List<Employee>> GetAllAsync(bool includeInactive = false)
    {
        var query = InMemoryDataStore.Employees.AsEnumerable();
        if (!includeInactive)
        {
            query = query.Where(e => e.Status != StatusCode.Rejected);
        }
        var results = query.ToList();
        if (!includeInactive)
        {
            foreach (var employee in results)
            {
                employee.ContactPersons = employee.ContactPersons
                    .Where(c => c.Status != StatusCode.Rejected)
                    .ToList();
            }
        }
        return Task.FromResult(results);
    }

    public Task AddAsync(Employee employee)
    {
        employee.Id = InMemoryDataStore.NextEmployeeId();
        AssignContactIds(employee);
        InMemoryDataStore.Employees.Add(employee);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Employee employee)
    {
        var index = InMemoryDataStore.Employees.FindIndex(e => e.Id == employee.Id);
        if (index >= 0)
        {
            AssignContactIds(employee);
            InMemoryDataStore.Employees[index] = employee;
        }
        return Task.CompletedTask;
    }

    private static void AssignContactIds(Employee employee)
    {
        foreach (var contact in employee.ContactPersons)
        {
            if (contact.Id == 0)
            {
                contact.Id = InMemoryDataStore.NextContactId();
            }
        }
    }
}
