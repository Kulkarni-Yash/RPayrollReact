using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Interfaces.Services;

public interface IPayrollService
{
    Task<Payroll> GeneratePayrollAsync(int employeeId, DateTime periodStart, DateTime periodEnd);
    Task<IEnumerable<Payroll>> GetPayrollsByEmployeeAsync(int employeeId);
    Task<IEnumerable<Payroll>> GetPayrollsForCurrentUserAsync();
    Task<bool> DeletePayrollAsync(int payrollId);
}
