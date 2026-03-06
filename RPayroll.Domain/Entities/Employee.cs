using RPayroll.Domain.Common;

namespace RPayroll.Domain.Entities;

public class Employee : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public decimal BasicSalary { get; set; }
    public DateTime? DateOfJoining { get; set; }
    public string? Address { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? IFSCCode { get; set; }

    public int TotalSickLeaves { get; set; }
    public int UsedSickLeaves { get; set; }
    public int TotalCasualLeaves { get; set; }
    public int UsedCasualLeaves { get; set; }
    public int TotalGovernmentLeaves { get; set; }
    public int UsedGovernmentLeaves { get; set; }
    public int TotalUnpaidLeaves { get; set; }
    public int UsedUnpaidLeaves { get; set; }

    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public List<Employee> Subordinates { get; set; } = new();

    public int? UserId { get; set; }
    public User? User { get; set; }

    public List<EmployeeContactPerson> ContactPersons { get; set; } = new();
    public List<LeaveRequest> LeaveRequests { get; set; } = new();
    public List<Payroll> Payrolls { get; set; } = new();
    public List<Attendance> Attendances { get; set; } = new();
}
