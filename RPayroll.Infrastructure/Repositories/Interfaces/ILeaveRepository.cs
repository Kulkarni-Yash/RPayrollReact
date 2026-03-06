using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Repositories.Interfaces;

public interface ILeaveRepository
{
    Task<LeaveRequest?> GetByIdAsync(int id, bool includeInactive = false);
    Task<List<LeaveRequest>> GetAllAsync(bool includeInactive = false);
    Task<List<LeaveRequest>> GetByEmployeeAsync(int employeeId, bool includeInactive = false);
    Task AddAsync(LeaveRequest leaveRequest);
    Task UpdateAsync(LeaveRequest leaveRequest);
}
