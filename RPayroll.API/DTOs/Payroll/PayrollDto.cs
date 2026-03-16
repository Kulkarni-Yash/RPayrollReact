namespace RPayroll.API.DTOs.Payroll;

public class PayrollDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime GeneratedDate { get; set; }
}
