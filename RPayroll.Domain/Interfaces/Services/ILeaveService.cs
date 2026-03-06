using RPayroll.Domain.DTOs.Leave;

namespace RPayroll.Domain.Interfaces.Services;

public interface ILeaveService
{
    Task<LeaveRequestDto> ApplyLeaveAsync(LeaveRequestDto dto);
    Task<LeaveRequestDto?> ApproveLeaveAsync(int leaveId);
    Task<LeaveRequestDto?> RejectLeaveAsync(int leaveId);
    Task<IEnumerable<LeaveRequestDto>> GetLeavesByEmployeeAsync(int employeeId);
    Task<IEnumerable<LeaveRequestDto>> GetLeavesForCurrentUserAsync();
    Task<IEnumerable<LeaveRequestDto>> GetMyLeavesAsync();
    Task<bool> SoftDeleteLeaveAsync(int leaveId);
}
