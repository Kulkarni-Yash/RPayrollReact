namespace RPayroll.Domain.DTOs.Employee;

public class UpdateEmployeeProfileDto
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? IFSCCode { get; set; }
}
