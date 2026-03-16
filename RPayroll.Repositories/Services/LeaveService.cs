using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Interfaces.Common;
using RPayroll.Infrastructure.Interfaces.Services;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.Repositories.Services;

public class LeaveService : ILeaveService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public LeaveService(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<LeaveRequest> ApplyLeaveAsync(LeaveRequest leaveRequest)
    {
        EnsureAuthenticated();
        EnsureCanApplyLeave(leaveRequest.EmployeeId);

        var employee = await _unitOfWork.Employees.GetByIdAsync(leaveRequest.EmployeeId)
            ?? throw new InvalidOperationException("Employee not found.");

        var existingLeaves = await _unitOfWork.Leaves.GetByEmployeeAsync(leaveRequest.EmployeeId, includeInactive: true);
        var hasOverlap = existingLeaves.Any(l =>
            l.Status != StatusCode.Rejected &&
            leaveRequest.StartDate <= l.EndDate &&
            leaveRequest.EndDate >= l.StartDate);

        if (hasOverlap)
        {
            throw new InvalidOperationException("Overlapping leave request exists.");
        }

        leaveRequest.Employee = employee;
        leaveRequest.CreatedDate = DateTime.UtcNow;
        leaveRequest.Status = StatusCode.Pending;

        await _unitOfWork.Leaves.AddAsync(leaveRequest);
        await _unitOfWork.SaveChangesAsync();
        return leaveRequest;
    }

    public async Task<LeaveRequest?> ApproveLeaveAsync(int leaveId)
    {
        EnsureAuthenticated();
        var leave = await _unitOfWork.Leaves.GetByIdAsync(leaveId, includeInactive: true);
        if (leave == null)
        {
            return null;
        }

        EnsureCanApproveLeave(leave);
        leave.Status = StatusCode.Accepted;
        leave.ApprovedByUserId = _currentUser.UserId;
        leave.ApprovedByUser = await _unitOfWork.Users.GetByIdAsync(_currentUser.UserId, includeInactive: true);
        leave.ApprovedDate = DateTime.UtcNow;
        leave.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.Leaves.UpdateAsync(leave);
        await _unitOfWork.SaveChangesAsync();
        return leave;
    }

    public async Task<LeaveRequest?> RejectLeaveAsync(int leaveId)
    {
        EnsureAuthenticated();
        var leave = await _unitOfWork.Leaves.GetByIdAsync(leaveId, includeInactive: true);
        if (leave == null)
        {
            return null;
        }

        EnsureCanApproveLeave(leave);
        leave.Status = StatusCode.Rejected;
        leave.ApprovedByUserId = _currentUser.UserId;
        leave.ApprovedByUser = await _unitOfWork.Users.GetByIdAsync(_currentUser.UserId, includeInactive: true);
        leave.ApprovedDate = DateTime.UtcNow;
        leave.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.Leaves.UpdateAsync(leave);
        await _unitOfWork.SaveChangesAsync();
        return leave;
    }

    public async Task<IEnumerable<LeaveRequest>> GetLeavesByEmployeeAsync(int employeeId)
    {
        EnsureAuthenticated();
        if (!await CanViewEmployeeAsync(employeeId))
        {
            throw new UnauthorizedAccessException("Not allowed to view leaves.");
        }

        return await _unitOfWork.Leaves.GetByEmployeeAsync(employeeId, includeInactive: true);
    }

    public async Task<IEnumerable<LeaveRequest>> GetLeavesForCurrentUserAsync()
    {
        EnsureAuthenticated();
        var leaves = await _unitOfWork.Leaves.GetAllAsync(includeInactive: true);
        return await ApplyLeaveVisibilityAsync(leaves);
    }

    public async Task<IEnumerable<LeaveRequest>> GetMyLeavesAsync()
    {
        EnsureAuthenticated();
        if (!_currentUser.EmployeeId.HasValue)
        {
            return Enumerable.Empty<LeaveRequest>();
        }

        return await _unitOfWork.Leaves.GetByEmployeeAsync(_currentUser.EmployeeId.Value, includeInactive: true);
    }

    public async Task<bool> SoftDeleteLeaveAsync(int leaveId)
    {
        EnsureAuthenticated();
        var leave = await _unitOfWork.Leaves.GetByIdAsync(leaveId, includeInactive: true);
        if (leave == null)
        {
            return false;
        }

        if (leave.Status != StatusCode.Pending)
        {
            throw new InvalidOperationException("Only pending leave can be deleted.");
        }

        if (!await CanViewEmployeeAsync(leave.EmployeeId))
        {
            throw new UnauthorizedAccessException("Not allowed to delete leave.");
        }

        leave.Status = StatusCode.Rejected;
        leave.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.Leaves.UpdateAsync(leave);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task<IEnumerable<LeaveRequest>> ApplyLeaveVisibilityAsync(IEnumerable<LeaveRequest> leaves)
    {
        if (IsAdmin() || IsHr())
        {
            return leaves;
        }

        if (IsManager())
        {
            var managerEmployeeId = _currentUser.EmployeeId ?? 0;
            var employees = await _unitOfWork.Employees.GetAllAsync(includeInactive: false);
            var subordinateIds = employees.Where(e => e.ManagerId == managerEmployeeId).Select(e => e.Id).ToHashSet();
            return leaves.Where(l => subordinateIds.Contains(l.EmployeeId) || l.EmployeeId == managerEmployeeId).ToList();
        }

        if (_currentUser.EmployeeId.HasValue)
        {
            return leaves.Where(l => l.EmployeeId == _currentUser.EmployeeId.Value).ToList();
        }

        return Enumerable.Empty<LeaveRequest>();
    }

    private void EnsureCanApplyLeave(int employeeId)
    {
        if (IsAdmin() || IsHr())
        {
            return;
        }

        if (_currentUser.EmployeeId.HasValue && _currentUser.EmployeeId.Value == employeeId)
        {
            return;
        }

        throw new UnauthorizedAccessException("Not allowed to apply leave for this employee.");
    }

    private void EnsureCanApproveLeave(LeaveRequest leave)
    {
        if (IsAdmin() || IsHr())
        {
            return;
        }

        if (IsManager())
        {
            var managerEmployeeId = _currentUser.EmployeeId ?? 0;
            if (leave.Employee?.ManagerId == managerEmployeeId || leave.EmployeeId == managerEmployeeId)
            {
                return;
            }
        }

        throw new UnauthorizedAccessException("Not allowed to approve leave.");
    }

    private async Task<bool> CanViewEmployeeAsync(int employeeId)
    {
        if (IsAdmin() || IsHr())
        {
            return true;
        }

        if (_currentUser.EmployeeId.HasValue && _currentUser.EmployeeId.Value == employeeId)
        {
            return true;
        }

        if (IsManager())
        {
            var managerEmployeeId = _currentUser.EmployeeId ?? 0;
            var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, includeInactive: true);
            return employee != null && employee.ManagerId == managerEmployeeId;
        }

        return false;
    }

    private bool IsAdmin() => string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);
    private bool IsHr() => string.Equals(_currentUser.Role, "HR", StringComparison.OrdinalIgnoreCase);
    private bool IsManager() => string.Equals(_currentUser.Role, "Manager", StringComparison.OrdinalIgnoreCase);

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }
    }
}
