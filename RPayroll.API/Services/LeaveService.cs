using RPayroll.Domain.DTOs.Leave;
using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Domain.Interfaces.Services;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.API.Services;

public class LeaveService : ILeaveService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public LeaveService(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<LeaveRequestDto> ApplyLeaveAsync(LeaveRequestDto dto)
    {
        EnsureAuthenticated();
        EnsureCanApplyLeave(dto.EmployeeId);

        var employee = await _unitOfWork.Employees.GetByIdAsync(dto.EmployeeId);
        if (employee == null)
        {
            throw new InvalidOperationException("Employee not found.");
        }

        var existingLeaves = await _unitOfWork.Leaves.GetByEmployeeAsync(dto.EmployeeId, includeInactive: true);
        var hasOverlap = existingLeaves.Any(l =>
            l.Status != StatusCode.Rejected &&
            dto.StartDate <= l.EndDate &&
            dto.EndDate >= l.StartDate);

        if (hasOverlap)
        {
            throw new InvalidOperationException("Overlapping leave request exists.");
        }

        var leave = new LeaveRequest
        {
            EmployeeId = dto.EmployeeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Reason = dto.Reason,
            LeaveType = dto.LeaveType,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Pending
        };

        await _unitOfWork.Leaves.AddAsync(leave);
        await _unitOfWork.SaveChangesAsync();

        return MapLeave(leave);
    }

    public async Task<LeaveRequestDto?> ApproveLeaveAsync(int leaveId)
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

        return MapLeave(leave);
    }

    public async Task<LeaveRequestDto?> RejectLeaveAsync(int leaveId)
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

        return MapLeave(leave);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetLeavesByEmployeeAsync(int employeeId)
    {
        EnsureAuthenticated();
        if (!await CanViewEmployeeAsync(employeeId))
        {
            throw new UnauthorizedAccessException("Not allowed to view leaves.");
        }

        var leaves = await _unitOfWork.Leaves.GetByEmployeeAsync(employeeId, includeInactive: true);
        return leaves.Select(MapLeave).ToList();
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetLeavesForCurrentUserAsync()
    {
        EnsureAuthenticated();
        var leaves = await _unitOfWork.Leaves.GetAllAsync(includeInactive: true);
        var filtered = await ApplyLeaveVisibilityAsync(leaves);
        return filtered.Select(MapLeave).ToList();
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetMyLeavesAsync()
    {
        EnsureAuthenticated();
        if (!_currentUser.EmployeeId.HasValue)
        {
            return Enumerable.Empty<LeaveRequestDto>();
        }

        var leaves = await _unitOfWork.Leaves.GetByEmployeeAsync(_currentUser.EmployeeId.Value, includeInactive: true);
        return leaves.Select(MapLeave).ToList();
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
            return leaves.Where(l => subordinateIds.Contains(l.EmployeeId) || l.EmployeeId == managerEmployeeId);
        }

        if (_currentUser.EmployeeId.HasValue)
        {
            return leaves.Where(l => l.EmployeeId == _currentUser.EmployeeId.Value);
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

    private static LeaveRequestDto MapLeave(LeaveRequest leave)
    {
        return new LeaveRequestDto
        {
            Id = leave.Id,
            EmployeeId = leave.EmployeeId,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            Reason = leave.Reason,
            LeaveType = leave.LeaveType,
            Status = leave.Status,
            ApprovedByUserId = leave.ApprovedByUserId,
            ApprovedDate = leave.ApprovedDate,
            ApprovedByName = leave.ApprovedByUser?.Username
        };
    }
}
