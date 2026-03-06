using RPayroll.Domain.Enums;

namespace RPayroll.Domain.DTOs.Attendance;

public class CreateAttendanceDto
{
    public int EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan? CheckInTime { get; set; }
    public TimeSpan? CheckOutTime { get; set; }
    public AttendanceStatus Status { get; set; }
}
