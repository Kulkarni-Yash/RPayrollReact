namespace RPayroll.Domain.DTOs.Employee;

public class EmployeeDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public decimal BasicSalary { get; set; }
    public int? ManagerId { get; set; }
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
    public List<EmployeeContactPersonDto> ContactPersons { get; set; } = new();
}
