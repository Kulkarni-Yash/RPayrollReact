using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;

namespace RPayroll.Infrastructure;

public static class InMemoryDataStore
{
    public static List<Employee> Employees { get; } = new();
    public static List<User> Users { get; } = new();
    public static List<LeaveRequest> LeaveRequests { get; } = new();
    public static List<Payroll> Payrolls { get; } = new();
    public static List<Role> Roles { get; } = new();
    public static List<Attendance> Attendances { get; } = new();

    private static int _employeeId = 1;
    private static int _userId = 1;
    private static int _leaveId = 1;
    private static int _payrollId = 1;
    private static int _roleId = 1;
    private static int _contactId = 1;
    private static int _attendanceId = 1;

    static InMemoryDataStore()
    {
        var adminRole = new Role
        {
            Id = NextRoleId(),
            Name = "Admin",
            HierarchyLevel = 1,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };
        var hrRole = new Role
        {
            Id = NextRoleId(),
            Name = "HR",
            HierarchyLevel = 2,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };
        var managerRole = new Role
        {
            Id = NextRoleId(),
            Name = "Manager",
            HierarchyLevel = 3,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };
        var employeeRole = new Role
        {
            Id = NextRoleId(),
            Name = "Employee",
            HierarchyLevel = 4,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };

        Roles.AddRange(new[] { adminRole, hrRole, managerRole, employeeRole });

        var adminUser = new User
        {
            Id = NextUserId(),
            Username = "admin",
            Password = "admin123",
            RoleId = adminRole.Id,
            Role = adminRole,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };

        var hrUser = new User
        {
            Id = NextUserId(),
            Username = "hr",
            Password = "hr123",
            RoleId = hrRole.Id,
            Role = hrRole,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };

        var managerEmployee = new Employee
        {
            Id = NextEmployeeId(),
            FirstName = "Mark",
            LastName = "Manager",
            Department = "Operations",
            Position = "Manager",
            BasicSalary = 80000,
            DateOfJoining = DateTime.UtcNow.Date.AddMonths(-6),
            Address = "100 Market Street",
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };

        var managerUser = new User
        {
            Id = NextUserId(),
            Username = "manager",
            Password = "manager123",
            RoleId = managerRole.Id,
            Role = managerRole,
            EmployeeId = managerEmployee.Id,
            Employee = managerEmployee,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };
        managerEmployee.UserId = managerUser.Id;
        managerEmployee.User = managerUser;

        var employeeOne = new Employee
        {
            Id = NextEmployeeId(),
            FirstName = "Eve",
            LastName = "Worker",
            Department = "Operations",
            Position = "Associate",
            BasicSalary = 45000,
            DateOfJoining = DateTime.UtcNow.Date.AddMonths(-2),
            Address = "200 Center Road",
            ManagerId = managerEmployee.Id,
            Manager = managerEmployee,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };

        var employeeTwo = new Employee
        {
            Id = NextEmployeeId(),
            FirstName = "Sam",
            LastName = "Staff",
            Department = "Operations",
            Position = "Associate",
            BasicSalary = 46000,
            DateOfJoining = DateTime.UtcNow.Date.AddMonths(-1),
            Address = "300 Riverside Ave",
            ManagerId = managerEmployee.Id,
            Manager = managerEmployee,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };

        managerEmployee.Subordinates.AddRange(new[] { employeeOne, employeeTwo });

        var employeeUser = new User
        {
            Id = NextUserId(),
            Username = "employee",
            Password = "employee123",
            RoleId = employeeRole.Id,
            Role = employeeRole,
            EmployeeId = employeeOne.Id,
            Employee = employeeOne,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };
        employeeOne.UserId = employeeUser.Id;
        employeeOne.User = employeeUser;

        Users.AddRange(new[] { adminUser, hrUser, managerUser, employeeUser });
        adminRole.Users.Add(adminUser);
        hrRole.Users.Add(hrUser);
        managerRole.Users.Add(managerUser);
        employeeRole.Users.Add(employeeUser);

        Employees.AddRange(new[] { managerEmployee, employeeOne, employeeTwo });

        var pendingLeave = new LeaveRequest
        {
            Id = NextLeaveId(),
            EmployeeId = employeeOne.Id,
            Employee = employeeOne,
            StartDate = DateTime.UtcNow.Date.AddDays(2),
            EndDate = DateTime.UtcNow.Date.AddDays(4),
            Reason = "Medical",
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Pending
        };

        var approvedLeave = new LeaveRequest
        {
            Id = NextLeaveId(),
            EmployeeId = employeeTwo.Id,
            Employee = employeeTwo,
            StartDate = DateTime.UtcNow.Date.AddDays(-5),
            EndDate = DateTime.UtcNow.Date.AddDays(-3),
            Reason = "Vacation",
            CreatedDate = DateTime.UtcNow.AddDays(-7),
            ApprovedByUserId = managerUser.Id,
            ApprovedByUser = managerUser,
            ApprovedDate = DateTime.UtcNow.AddDays(-6),
            Status = StatusCode.Accepted
        };

        LeaveRequests.AddRange(new[] { pendingLeave, approvedLeave });

        var today = DateTime.UtcNow.Date;
        Attendances.AddRange(new[]
        {
            new Attendance
            {
                AttendanceId = NextAttendanceId(),
                EmployeeId = managerEmployee.Id,
                Employee = managerEmployee,
                Date = today,
                CheckInTime = new TimeSpan(9, 0, 0),
                CheckOutTime = new TimeSpan(18, 0, 0),
                Status = AttendanceStatus.Present,
                CreatedDate = DateTime.UtcNow
            },
            new Attendance
            {
                AttendanceId = NextAttendanceId(),
                EmployeeId = employeeOne.Id,
                Employee = employeeOne,
                Date = today,
                CheckInTime = new TimeSpan(9, 15, 0),
                CheckOutTime = new TimeSpan(18, 0, 0),
                Status = AttendanceStatus.Present,
                CreatedDate = DateTime.UtcNow
            },
            new Attendance
            {
                AttendanceId = NextAttendanceId(),
                EmployeeId = employeeTwo.Id,
                Employee = employeeTwo,
                Date = today,
                Status = AttendanceStatus.Absent,
                CreatedDate = DateTime.UtcNow
            }
        });
    }

    public static int NextEmployeeId() => _employeeId++;
    public static int NextUserId() => _userId++;
    public static int NextLeaveId() => _leaveId++;
    public static int NextPayrollId() => _payrollId++;
    public static int NextRoleId() => _roleId++;
    public static int NextContactId() => _contactId++;
    public static int NextAttendanceId() => _attendanceId++;
}
