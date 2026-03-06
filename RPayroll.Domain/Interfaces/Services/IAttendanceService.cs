using RPayroll.Domain.DTOs.Attendance;

namespace RPayroll.Domain.Interfaces.Services;

public interface IAttendanceService
{
    Task<AttendanceDto> CreateAttendanceAsync(CreateAttendanceDto dto);
    Task<AttendanceDto?> UpdateAttendanceAsync(UpdateAttendanceDto dto);
    Task<IEnumerable<AttendanceDto>> GetByEmployeeAsync(int employeeId);
    Task<IEnumerable<AttendanceDto>> GetByMonthAsync(int employeeId, int month, int year);
    Task<IEnumerable<AttendanceDto>> GetByDateAsync(DateTime date);
    Task<IEnumerable<AttendanceDto>> GetMyAttendanceAsync();
}
