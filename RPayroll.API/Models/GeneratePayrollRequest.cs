namespace RPayroll.API.Models;

public class GeneratePayrollRequest
{
    public int EmployeeId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}
