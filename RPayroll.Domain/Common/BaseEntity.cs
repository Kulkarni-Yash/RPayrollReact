using RPayroll.Domain.Enums;

namespace RPayroll.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public StatusCode Status { get; set; } = StatusCode.Pending;
}
