using RPayroll.API.DTOs.Attendance;
using RPayroll.API.DTOs.Employee;
using RPayroll.API.DTOs.Leave;
using RPayroll.API.DTOs.Role;
using RPayroll.API.DTOs.User;

namespace RPayroll.API.Metadata;

public static class FormModelRegistry
{
    public static IReadOnlyList<FormModelRegistration> All { get; } =
    [
        new("employee", "Employee", typeof(EmployeeDto), "/api/employees", "POST"),
        new("leave", "Leave", typeof(LeaveRequestDto), "/api/leave", "POST"),
        new("attendance", "Attendance", typeof(AttendanceDto), "/api/attendance", "POST"),
        new("role", "Role", typeof(RoleDto), "/api/roles", "POST"),
        new("user", "User", typeof(UserDto), "/api/users", "POST")
    ];
}

public sealed record FormModelRegistration(
    string Key,
    string Label,
    Type DtoType,
    string SubmitUrl,
    string SubmitMethod);
