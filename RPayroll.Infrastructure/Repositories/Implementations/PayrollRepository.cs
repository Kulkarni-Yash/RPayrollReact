using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Repositories.Interfaces;

namespace RPayroll.Infrastructure.Repositories.Implementations;

public class PayrollRepository : IPayrollRepository
{
    public Task<Payroll?> GetByIdAsync(int id, bool includeInactive = false)
    {
        var payroll = InMemoryDataStore.Payrolls.FirstOrDefault(p =>
            p.Id == id && (includeInactive || p.Status != StatusCode.Rejected));
        return Task.FromResult(payroll);
    }

    public Task<List<Payroll>> GetAllAsync(bool includeInactive = false)
    {
        var query = InMemoryDataStore.Payrolls.AsEnumerable();
        if (!includeInactive)
        {
            query = query.Where(p => p.Status != StatusCode.Rejected);
        }
        return Task.FromResult(query.ToList());
    }

    public Task<List<Payroll>> GetByEmployeeAsync(int employeeId, bool includeInactive = false)
    {
        var query = InMemoryDataStore.Payrolls.Where(p => p.EmployeeId == employeeId);
        if (!includeInactive)
        {
            query = query.Where(p => p.Status != StatusCode.Rejected);
        }
        var payrolls = query.ToList();
        return Task.FromResult(payrolls);
    }

    public Task AddAsync(Payroll payroll)
    {
        payroll.Id = InMemoryDataStore.NextPayrollId();
        InMemoryDataStore.Payrolls.Add(payroll);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Payroll payroll)
    {
        var index = InMemoryDataStore.Payrolls.FindIndex(p => p.Id == payroll.Id);
        if (index >= 0)
        {
            InMemoryDataStore.Payrolls[index] = payroll;
        }
        return Task.CompletedTask;
    }
}
