using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Interfaces.Common;
using RPayroll.Infrastructure.Interfaces.Services;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.Repositories.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public AttendanceService(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Attendance> CreateAttendanceAsync(Attendance attendance)
    {
        EnsureAuthenticated();
        ValidateTimes(attendance.CheckInTime, attendance.CheckOutTime);

        var today = DateTime.Today;
        var isEmployee = IsEmployee();

        if (isEmployee)
        {
            if (!_currentUser.EmployeeId.HasValue)
            {
                throw new UnauthorizedAccessException("Employee id not found.");
            }

            attendance.EmployeeId = _currentUser.EmployeeId.Value;
            if (attendance.Date.Date != today)
            {
                throw new InvalidOperationException("Employees can only mark attendance for today.");
            }
        }

        if (!isEmployee && !IsAdmin() && !IsHr())
        {
            throw new UnauthorizedAccessException("Admin/HR required to mark attendance.");
        }

        var existing = await _unitOfWork.Attendances.GetByDateRangeAsync(attendance.EmployeeId, attendance.Date.Date, attendance.Date.Date);
        if (existing.Any())
        {
            throw new InvalidOperationException("Attendance already exists for this date.");
        }

        attendance.Date = attendance.Date.Date;
        attendance.CreatedDate = DateTime.UtcNow;
        await _unitOfWork.Attendances.AddAsync(attendance);
        await _unitOfWork.SaveChangesAsync();

        attendance.Employee = await _unitOfWork.Employees.GetByIdAsync(attendance.EmployeeId, includeInactive: true);
        return attendance;
    }

    public async Task<Attendance?> UpdateAttendanceAsync(Attendance attendance)
    {
        EnsureAuthenticated();
        ValidateTimes(attendance.CheckInTime, attendance.CheckOutTime);

        var existingAttendance = await _unitOfWork.Attendances.GetByIdAsync(attendance.AttendanceId);
        if (existingAttendance == null)
        {
            return null;
        }

        if (IsEmployee())
        {
            if (!_currentUser.EmployeeId.HasValue)
            {
                throw new UnauthorizedAccessException("Employee id not found.");
            }

            var employeeId = _currentUser.EmployeeId.Value;
            if (existingAttendance.EmployeeId != employeeId)
            {
                throw new UnauthorizedAccessException("Not allowed to update attendance.");
            }

            var today = DateTime.Today;
            if (existingAttendance.Date.Date != today)
            {
                throw new InvalidOperationException("Employees can only update today's attendance.");
            }

            attendance.EmployeeId = employeeId;
            attendance.Date = existingAttendance.Date;
        }
        else
        {
            EnsureAdminOrHr();
        }

        existingAttendance.EmployeeId = attendance.EmployeeId;
        existingAttendance.Date = attendance.Date.Date;
        existingAttendance.CheckInTime = attendance.CheckInTime;
        existingAttendance.CheckOutTime = attendance.CheckOutTime;
        existingAttendance.Status = attendance.Status;
        existingAttendance.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Attendances.UpdateAsync(existingAttendance);
        await _unitOfWork.SaveChangesAsync();

        existingAttendance.Employee = await _unitOfWork.Employees.GetByIdAsync(existingAttendance.EmployeeId, includeInactive: true);
        return existingAttendance;
    }

    public async Task<IEnumerable<Attendance>> GetByEmployeeAsync(int employeeId)
    {
        EnsureAuthenticated();
        if (!await CanViewEmployeeAsync(employeeId))
        {
            throw new UnauthorizedAccessException("Not allowed to view attendance.");
        }

        var items = await _unitOfWork.Attendances.GetByEmployeeAsync(employeeId);
        return await HydrateAsync(items);
    }

    public async Task<IEnumerable<Attendance>> GetByMonthAsync(int employeeId, int month, int year)
    {
        EnsureAuthenticated();
        if (!await CanViewEmployeeAsync(employeeId))
        {
            throw new UnauthorizedAccessException("Not allowed to view attendance.");
        }

        var items = await _unitOfWork.Attendances.GetByMonthAsync(employeeId, month, year);
        return await HydrateAsync(items);
    }

    public async Task<IEnumerable<Attendance>> GetByDateAsync(DateTime date)
    {
        EnsureAuthenticated();
        EnsureAdminOrHr();
        var items = await _unitOfWork.Attendances.GetByDateAsync(date.Date);
        return await HydrateAsync(items);
    }

    public async Task<IEnumerable<Attendance>> GetMyAttendanceAsync()
    {
        EnsureAuthenticated();
        if (!_currentUser.EmployeeId.HasValue)
        {
            return Enumerable.Empty<Attendance>();
        }

        var items = await _unitOfWork.Attendances.GetByEmployeeAsync(_currentUser.EmployeeId.Value);
        return await HydrateAsync(items);
    }

    private async Task<IEnumerable<Attendance>> HydrateAsync(IEnumerable<Attendance> items)
    {
        var list = items.ToList();
        foreach (var attendance in list)
        {
            attendance.Employee = await _unitOfWork.Employees.GetByIdAsync(attendance.EmployeeId, includeInactive: true);
        }
        return list;
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

    private static void ValidateTimes(TimeSpan? checkIn, TimeSpan? checkOut)
    {
        if (checkIn.HasValue && checkOut.HasValue && checkOut.Value <= checkIn.Value)
        {
            throw new InvalidOperationException("Check-out time must be greater than check-in time.");
        }
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }
    }

    private void EnsureAdminOrHr()
    {
        if (!IsAdmin() && !IsHr())
        {
            throw new UnauthorizedAccessException("Admin or HR required.");
        }
    }

    private bool IsAdmin() => string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);
    private bool IsHr() => string.Equals(_currentUser.Role, "HR", StringComparison.OrdinalIgnoreCase);
    private bool IsManager() => string.Equals(_currentUser.Role, "Manager", StringComparison.OrdinalIgnoreCase);
    private bool IsEmployee() => string.Equals(_currentUser.Role, "Employee", StringComparison.OrdinalIgnoreCase);
}
