using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Interfaces.Common;
using RPayroll.Infrastructure.Interfaces.Services;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.Repositories.Services;

public class PayrollService : IPayrollService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public PayrollService(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Payroll> GeneratePayrollAsync(int employeeId, DateTime periodStart, DateTime periodEnd)
    {
        EnsureAuthenticated();
        if (!await CanViewEmployeeAsync(employeeId))
        {
            throw new UnauthorizedAccessException("Not allowed to generate payroll.");
        }

        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, includeInactive: true)
            ?? throw new InvalidOperationException("Employee not found.");

        var monthStart = new DateTime(periodStart.Year, periodStart.Month, 1);
        var totalDaysInMonth = DateTime.DaysInMonth(periodStart.Year, periodStart.Month);
        var monthEnd = monthStart.AddDays(totalDaysInMonth - 1);
        var effectiveEnd = periodEnd.Date > monthEnd ? monthEnd : periodEnd.Date;

        var joiningDate = employee.DateOfJoining?.Date;
        if (joiningDate.HasValue && joiningDate.Value > effectiveEnd)
        {
            return await CreateZeroPayrollAsync(employeeId, periodStart, periodEnd);
        }

        var rangeStart = joiningDate.HasValue && joiningDate.Value > monthStart ? joiningDate.Value : monthStart;
        var attendanceRecords = await _unitOfWork.Attendances.GetByDateRangeAsync(employeeId, rangeStart, effectiveEnd);
        var attendanceByDate = attendanceRecords.ToDictionary(a => a.Date.Date, a => a);

        var approvedLeaves = await _unitOfWork.Leaves.GetByEmployeeAsync(employeeId, includeInactive: false);
        var approvedLeaveDates = ExpandApprovedLeaveDates(approvedLeaves, rangeStart, effectiveEnd);

        decimal payableDays = 0;
        for (var date = rangeStart; date <= effectiveEnd; date = date.AddDays(1))
        {
            if (attendanceByDate.TryGetValue(date, out var attendance))
            {
                payableDays += attendance.Status switch
                {
                    AttendanceStatus.Present => 1m,
                    AttendanceStatus.HalfDay => 0.5m,
                    AttendanceStatus.Holiday => 1m,
                    AttendanceStatus.Leave => 1m,
                    _ => 0m
                };
            }
            else if (approvedLeaveDates.Contains(date))
            {
                payableDays += 1m;
            }
        }

        var totalDays = DateTime.DaysInMonth(periodStart.Year, periodStart.Month);
        var dailyRate = totalDays == 0 ? 0 : employee.BasicSalary / totalDays;
        var payroll = new Payroll
        {
            EmployeeId = employeeId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            GrossSalary = employee.BasicSalary,
            NetSalary = dailyRate * payableDays,
            GeneratedDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };

        await _unitOfWork.Payrolls.AddAsync(payroll);
        await _unitOfWork.SaveChangesAsync();
        return payroll;
    }

    public async Task<IEnumerable<Payroll>> GetPayrollsByEmployeeAsync(int employeeId)
    {
        EnsureAuthenticated();
        if (!await CanViewEmployeeAsync(employeeId))
        {
            throw new UnauthorizedAccessException("Not allowed to view payrolls.");
        }

        return await _unitOfWork.Payrolls.GetByEmployeeAsync(employeeId, includeInactive: false);
    }

    public async Task<IEnumerable<Payroll>> GetPayrollsForCurrentUserAsync()
    {
        EnsureAuthenticated();
        var payrolls = await _unitOfWork.Payrolls.GetAllAsync(includeInactive: false);
        return await ApplyVisibilityFilterAsync(payrolls);
    }

    public async Task<bool> DeletePayrollAsync(int payrollId)
    {
        EnsureAuthenticated();
        if (!IsAdmin())
        {
            throw new UnauthorizedAccessException("Only Admin can delete payroll.");
        }

        var payroll = await _unitOfWork.Payrolls.GetByIdAsync(payrollId, includeInactive: true);
        if (payroll == null)
        {
            return false;
        }

        payroll.Status = StatusCode.Rejected;
        payroll.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.Payrolls.UpdateAsync(payroll);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task<IEnumerable<Payroll>> ApplyVisibilityFilterAsync(IEnumerable<Payroll> payrolls)
    {
        if (IsAdmin() || IsHr())
        {
            return payrolls;
        }

        if (IsManager())
        {
            var managerEmployeeId = _currentUser.EmployeeId ?? 0;
            var employees = await _unitOfWork.Employees.GetAllAsync(includeInactive: false);
            var subordinateIds = employees.Where(e => e.ManagerId == managerEmployeeId).Select(e => e.Id).ToHashSet();
            return payrolls.Where(p => subordinateIds.Contains(p.EmployeeId)).ToList();
        }

        if (_currentUser.EmployeeId.HasValue)
        {
            return payrolls.Where(p => p.EmployeeId == _currentUser.EmployeeId.Value).ToList();
        }

        return Enumerable.Empty<Payroll>();
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

    private static HashSet<DateTime> ExpandApprovedLeaveDates(IEnumerable<LeaveRequest> leaves, DateTime rangeStart, DateTime rangeEnd)
    {
        var dates = new HashSet<DateTime>();
        foreach (var leave in leaves.Where(l => l.Status == StatusCode.Accepted))
        {
            var start = leave.StartDate.Date < rangeStart ? rangeStart : leave.StartDate.Date;
            var end = leave.EndDate.Date > rangeEnd ? rangeEnd : leave.EndDate.Date;
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                dates.Add(date);
            }
        }

        return dates;
    }

    private async Task<Payroll> CreateZeroPayrollAsync(int employeeId, DateTime periodStart, DateTime periodEnd)
    {
        var payroll = new Payroll
        {
            EmployeeId = employeeId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            GrossSalary = 0,
            NetSalary = 0,
            GeneratedDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };

        await _unitOfWork.Payrolls.AddAsync(payroll);
        await _unitOfWork.SaveChangesAsync();
        return payroll;
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
