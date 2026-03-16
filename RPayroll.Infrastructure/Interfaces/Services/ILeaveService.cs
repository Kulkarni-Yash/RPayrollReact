using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Interfaces.Services;

public interface ILeaveService
{
    Task<LeaveRequest> ApplyLeaveAsync(LeaveRequest leaveRequest);
    Task<LeaveRequest?> ApproveLeaveAsync(int leaveId);
    Task<LeaveRequest?> RejectLeaveAsync(int leaveId);
    Task<IEnumerable<LeaveRequest>> GetLeavesByEmployeeAsync(int employeeId);
    Task<IEnumerable<LeaveRequest>> GetLeavesForCurrentUserAsync();
    Task<IEnumerable<LeaveRequest>> GetMyLeavesAsync();
    Task<bool> SoftDeleteLeaveAsync(int leaveId);
}
