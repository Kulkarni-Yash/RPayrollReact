using RPayroll.Domain.DTOs.Attendance;
using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Domain.Interfaces.Services;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.API.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public AttendanceService(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<AttendanceDto> CreateAttendanceAsync(CreateAttendanceDto dto)
    {
        EnsureAuthenticated();
        ValidateTimes(dto.CheckInTime, dto.CheckOutTime);

        var today = DateTime.Today;
        var isEmployee = IsEmployee();

        if (isEmployee)
        {
            if (!_currentUser.EmployeeId.HasValue)
            {
                throw new UnauthorizedAccessException("Employee id not found.");
            }

            dto.EmployeeId = _currentUser.EmployeeId.Value;
            if (dto.Date.Date != today)
            {
                throw new InvalidOperationException("Employees can only mark attendance for today.");
            }
        }

        if (!isEmployee && !IsAdmin() && !IsHr())
        {
            throw new UnauthorizedAccessException("Admin/HR required to mark attendance.");
        }

        if (isEmployee && (dto.Date.Date < today || dto.Date.Date > today))
        {
            throw new InvalidOperationException("Invalid attendance date.");
        }

        var existing = await _unitOfWork.Attendances.GetByDateRangeAsync(dto.EmployeeId, dto.Date.Date, dto.Date.Date);
        if (existing.Any())
        {
            throw new InvalidOperationException("Attendance already exists for this date.");
        }

        var attendance = new Attendance
        {
            EmployeeId = dto.EmployeeId,
            Date = dto.Date.Date,
            CheckInTime = dto.CheckInTime,
            CheckOutTime = dto.CheckOutTime,
            Status = dto.Status,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.Attendances.AddAsync(attendance);
        await _unitOfWork.SaveChangesAsync();

        return await MapAttendanceAsync(attendance);
    }

    public async Task<AttendanceDto?> UpdateAttendanceAsync(UpdateAttendanceDto dto)
    {
        EnsureAuthenticated();
        EnsureAdminOrHr();
        ValidateTimes(dto.CheckInTime, dto.CheckOutTime);

        var attendance = await _unitOfWork.Attendances.GetByIdAsync(dto.AttendanceId);
        if (attendance == null)
        {
            return null;
        }

        attendance.EmployeeId = dto.EmployeeId;
        attendance.Date = dto.Date.Date;
        attendance.CheckInTime = dto.CheckInTime;
        attendance.CheckOutTime = dto.CheckOutTime;
        attendance.Status = dto.Status;
        attendance.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Attendances.UpdateAsync(attendance);
        await _unitOfWork.SaveChangesAsync();

        return await MapAttendanceAsync(attendance);
    }

    public async Task<IEnumerable<AttendanceDto>> GetByEmployeeAsync(int employeeId)
    {
        EnsureAuthenticated();
        if (!await CanViewEmployeeAsync(employeeId))
        {
            throw new UnauthorizedAccessException("Not allowed to view attendance.");
        }

        var items = await _unitOfWork.Attendances.GetByEmployeeAsync(employeeId);
        return await MapAttendanceListAsync(items);
    }

    public async Task<IEnumerable<AttendanceDto>> GetByMonthAsync(int employeeId, int month, int year)
    {
        EnsureAuthenticated();
        if (!await CanViewEmployeeAsync(employeeId))
        {
            throw new UnauthorizedAccessException("Not allowed to view attendance.");
        }

        var items = await _unitOfWork.Attendances.GetByMonthAsync(employeeId, month, year);
        return await MapAttendanceListAsync(items);
    }

    public async Task<IEnumerable<AttendanceDto>> GetByDateAsync(DateTime date)
    {
        EnsureAuthenticated();
        EnsureAdminOrHr();
        var items = await _unitOfWork.Attendances.GetByDateAsync(date.Date);
        return await MapAttendanceListAsync(items);
    }

    public async Task<IEnumerable<AttendanceDto>> GetMyAttendanceAsync()
    {
        EnsureAuthenticated();
        if (!_currentUser.EmployeeId.HasValue)
        {
            return Enumerable.Empty<AttendanceDto>();
        }

        var items = await _unitOfWork.Attendances.GetByEmployeeAsync(_currentUser.EmployeeId.Value);
        return await MapAttendanceListAsync(items);
    }

    private async Task<List<AttendanceDto>> MapAttendanceListAsync(IEnumerable<Attendance> items)
    {
        var list = new List<AttendanceDto>();
        foreach (var attendance in items)
        {
            list.Add(await MapAttendanceAsync(attendance));
        }
        return list;
    }

    private async Task<AttendanceDto> MapAttendanceAsync(Attendance attendance)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(attendance.EmployeeId, includeInactive: true);
        var name = employee == null ? string.Empty : $"{employee.FirstName} {employee.LastName}".Trim();

        return new AttendanceDto
        {
            AttendanceId = attendance.AttendanceId,
            EmployeeId = attendance.EmployeeId,
            EmployeeName = name,
            Date = attendance.Date,
            CheckInTime = attendance.CheckInTime,
            CheckOutTime = attendance.CheckOutTime,
            Status = attendance.Status
        };
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
