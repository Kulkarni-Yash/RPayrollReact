using RPayroll.Domain.Common;

namespace RPayroll.Domain.Entities;

public class Payroll : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime GeneratedDate { get; set; }
}
