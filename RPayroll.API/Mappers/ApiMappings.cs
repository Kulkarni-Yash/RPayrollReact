using RPayroll.API.DTOs.Attendance;
using RPayroll.API.DTOs.Auth;
using RPayroll.API.DTOs.Employee;
using RPayroll.API.DTOs.Leave;
using RPayroll.API.DTOs.Payroll;
using RPayroll.API.DTOs.Role;
using RPayroll.API.DTOs.User;
using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;

namespace RPayroll.API.Mappers;

public static class ApiMappings
{
    public static Employee ToEntity(this EmployeeDto dto)
    {
        return new Employee
        {
            Id = dto.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            DateOfBirth = dto.DateOfBirth,
            Department = dto.Department,
            Position = dto.Position,
            BasicSalary = dto.BasicSalary,
            ManagerId = dto.ManagerId,
            DateOfJoining = dto.DateOfJoining,
            Address = dto.Address,
            BankName = dto.BankName,
            BankAccountNumber = dto.BankAccountNumber,
            IFSCCode = dto.IFSCCode,
            TotalSickLeaves = dto.TotalSickLeaves,
            UsedSickLeaves = dto.UsedSickLeaves,
            TotalCasualLeaves = dto.TotalCasualLeaves,
            UsedCasualLeaves = dto.UsedCasualLeaves,
            TotalGovernmentLeaves = dto.TotalGovernmentLeaves,
            UsedGovernmentLeaves = dto.UsedGovernmentLeaves,
            TotalUnpaidLeaves = dto.TotalUnpaidLeaves,
            UsedUnpaidLeaves = dto.UsedUnpaidLeaves,
            ContactPersons = dto.ContactPersons.Select(ToEntity).ToList()
        };
    }

    public static EmployeeContactPerson ToEntity(this EmployeeContactPersonDto dto)
    {
        return new EmployeeContactPerson
        {
            Id = dto.Id,
            EmployeeId = dto.EmployeeId,
            Name = dto.Name,
            Relationship = dto.Relationship,
            Phone = dto.Phone,
            Email = dto.Email,
            IsPrimary = dto.IsPrimary
        };
    }

    public static EmployeeDto ToDto(this Employee employee)
    {
        return new EmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Phone = employee.Phone,
            DateOfBirth = employee.DateOfBirth,
            Department = employee.Department,
            Position = employee.Position,
            BasicSalary = employee.BasicSalary,
            ManagerId = employee.ManagerId,
            DateOfJoining = employee.DateOfJoining,
            Address = employee.Address,
            BankName = employee.BankName,
            BankAccountNumber = employee.BankAccountNumber,
            IFSCCode = employee.IFSCCode,
            TotalSickLeaves = employee.TotalSickLeaves,
            UsedSickLeaves = employee.UsedSickLeaves,
            TotalCasualLeaves = employee.TotalCasualLeaves,
            UsedCasualLeaves = employee.UsedCasualLeaves,
            TotalGovernmentLeaves = employee.TotalGovernmentLeaves,
            UsedGovernmentLeaves = employee.UsedGovernmentLeaves,
            TotalUnpaidLeaves = employee.TotalUnpaidLeaves,
            UsedUnpaidLeaves = employee.UsedUnpaidLeaves,
            Username = employee.User?.Username,
            RoleName = employee.User?.Role?.Name,
            ContactPersons = employee.ContactPersons
                .Where(c => c.Status != StatusCode.Rejected)
                .Select(c => c.ToDto(employee.Id))
                .ToList()
        };
    }

    public static EmployeeContactPersonDto ToDto(this EmployeeContactPerson contact, int employeeId)
    {
        return new EmployeeContactPersonDto
        {
            Id = contact.Id,
            EmployeeId = employeeId,
            Name = contact.Name,
            Relationship = contact.Relationship,
            Phone = contact.Phone,
            Email = contact.Email,
            IsPrimary = contact.IsPrimary
        };
    }

    public static EmployeeSearchResultDto ToSearchDto(this Employee employee)
    {
        return new EmployeeSearchResultDto
        {
            Id = employee.Id,
            FullName = $"{employee.FirstName} {employee.LastName}".Trim()
        };
    }

    public static Attendance ToEntity(this AttendanceDto dto)
    {
        return new Attendance
        {
            AttendanceId = dto.AttendanceId,
            EmployeeId = dto.EmployeeId,
            Date = dto.Date,
            CheckInTime = dto.CheckInTime,
            CheckOutTime = dto.CheckOutTime,
            Status = dto.Status
        };
    }

    public static AttendanceDto ToDto(this Attendance attendance)
    {
        return new AttendanceDto
        {
            AttendanceId = attendance.AttendanceId,
            EmployeeId = attendance.EmployeeId,
            EmployeeName = attendance.Employee == null ? string.Empty : $"{attendance.Employee.FirstName} {attendance.Employee.LastName}".Trim(),
            Date = attendance.Date,
            CheckInTime = attendance.CheckInTime,
            CheckOutTime = attendance.CheckOutTime,
            Status = attendance.Status
        };
    }

    public static LeaveRequest ToEntity(this LeaveRequestDto dto)
    {
        return new LeaveRequest
        {
            Id = dto.Id,
            EmployeeId = dto.EmployeeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Reason = dto.Reason,
            LeaveType = dto.LeaveType,
            Status = dto.Status,
            ApprovedByUserId = dto.ApprovedByUserId,
            ApprovedDate = dto.ApprovedDate
        };
    }

    public static LeaveRequestDto ToDto(this LeaveRequest leave)
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

    public static PayrollDto ToDto(this Payroll payroll)
    {
        return new PayrollDto
        {
            Id = payroll.Id,
            EmployeeId = payroll.EmployeeId,
            PeriodStart = payroll.PeriodStart,
            PeriodEnd = payroll.PeriodEnd,
            GrossSalary = payroll.GrossSalary,
            NetSalary = payroll.NetSalary,
            GeneratedDate = payroll.GeneratedDate
        };
    }

    public static Role ToEntity(this RoleDto dto)
    {
        return new Role
        {
            Id = dto.Id,
            Name = dto.Name,
            HierarchyLevel = dto.HierarchyLevel,
            Status = (StatusCode)dto.Status
        };
    }

    public static RoleDto ToDto(this Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            HierarchyLevel = role.HierarchyLevel,
            Status = (int)role.Status
        };
    }

    public static User ToEntity(this UserDto dto)
    {
        return new User
        {
            Id = dto.Id,
            Username = dto.Username,
            Password = dto.Password ?? string.Empty,
            RoleId = dto.RoleId,
            EmployeeId = dto.EmployeeId,
            Status = dto.Status == 0 ? StatusCode.Accepted : (StatusCode)dto.Status
        };
    }

    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name ?? string.Empty,
            EmployeeId = user.EmployeeId,
            Status = (int)user.Status
        };
    }

    public static LoginResponseDto ToLoginResponseDto(this User user)
    {
        return new LoginResponseDto
        {
            Token = user.Token ?? string.Empty,
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role?.Name ?? string.Empty
        };
    }
}
