using RPayroll.Domain.Enums;

namespace RPayroll.Domain.DTOs.Leave;

public class LeaveRequestDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public string? LeaveType { get; set; }
    public StatusCode Status { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedByName { get; set; }
}
