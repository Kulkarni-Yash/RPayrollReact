using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Repositories.Interfaces;

public interface IAttendanceRepository
{
    Task<List<Attendance>> GetByEmployeeAsync(int employeeId);
    Task<List<Attendance>> GetByDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate);
    Task<List<Attendance>> GetByMonthAsync(int employeeId, int month, int year);
    Task<List<Attendance>> GetByDateAsync(DateTime date);
    Task<Attendance?> GetByIdAsync(int attendanceId);
    Task AddAsync(Attendance attendance);
    Task UpdateAsync(Attendance attendance);
    Task DeleteAsync(int attendanceId);
}
