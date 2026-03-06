using RPayroll.Domain.Common;

namespace RPayroll.Domain.Entities;

public class LeaveRequest : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public string? LeaveType { get; set; }

    public int? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }
    public DateTime? ApprovedDate { get; set; }
}
