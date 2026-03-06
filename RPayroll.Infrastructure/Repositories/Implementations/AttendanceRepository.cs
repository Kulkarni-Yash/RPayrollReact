using RPayroll.Domain.Entities;
using RPayroll.Infrastructure.Repositories.Interfaces;

namespace RPayroll.Infrastructure.Repositories.Implementations;

public class AttendanceRepository : IAttendanceRepository
{
    public Task<List<Attendance>> GetByEmployeeAsync(int employeeId)
    {
        var items = InMemoryDataStore.Attendances
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.Date)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<List<Attendance>> GetByDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        var items = InMemoryDataStore.Attendances
            .Where(a => a.EmployeeId == employeeId && a.Date.Date >= startDate.Date && a.Date.Date <= endDate.Date)
            .OrderByDescending(a => a.Date)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<List<Attendance>> GetByMonthAsync(int employeeId, int month, int year)
    {
        var items = InMemoryDataStore.Attendances
            .Where(a => a.EmployeeId == employeeId && a.Date.Month == month && a.Date.Year == year)
            .OrderByDescending(a => a.Date)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<List<Attendance>> GetByDateAsync(DateTime date)
    {
        var items = InMemoryDataStore.Attendances
            .Where(a => a.Date.Date == date.Date)
            .OrderBy(a => a.EmployeeId)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<Attendance?> GetByIdAsync(int attendanceId)
    {
        var item = InMemoryDataStore.Attendances.FirstOrDefault(a => a.AttendanceId == attendanceId);
        return Task.FromResult(item);
    }

    public Task AddAsync(Attendance attendance)
    {
        attendance.AttendanceId = InMemoryDataStore.NextAttendanceId();
        InMemoryDataStore.Attendances.Add(attendance);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Attendance attendance)
    {
        var index = InMemoryDataStore.Attendances.FindIndex(a => a.AttendanceId == attendance.AttendanceId);
        if (index >= 0)
        {
            InMemoryDataStore.Attendances[index] = attendance;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int attendanceId)
    {
        var index = InMemoryDataStore.Attendances.FindIndex(a => a.AttendanceId == attendanceId);
        if (index >= 0)
        {
            InMemoryDataStore.Attendances.RemoveAt(index);
        }
        return Task.CompletedTask;
    }
}
