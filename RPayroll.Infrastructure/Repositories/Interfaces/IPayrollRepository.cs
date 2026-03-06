using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Repositories.Interfaces;

public interface IPayrollRepository
{
    Task<Payroll?> GetByIdAsync(int id, bool includeInactive = false);
    Task<List<Payroll>> GetAllAsync(bool includeInactive = false);
    Task<List<Payroll>> GetByEmployeeAsync(int employeeId, bool includeInactive = false);
    Task AddAsync(Payroll payroll);
    Task UpdateAsync(Payroll payroll);
}
