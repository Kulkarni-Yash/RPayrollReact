using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Interfaces.Services;

public interface IAttendanceService
{
    Task<Attendance> CreateAttendanceAsync(Attendance attendance);
    Task<Attendance?> UpdateAttendanceAsync(Attendance attendance);
    Task<IEnumerable<Attendance>> GetByEmployeeAsync(int employeeId);
    Task<IEnumerable<Attendance>> GetByMonthAsync(int employeeId, int month, int year);
    Task<IEnumerable<Attendance>> GetByDateAsync(DateTime date);
    Task<IEnumerable<Attendance>> GetMyAttendanceAsync();
}
