using RPayroll.Domain.Common;

namespace RPayroll.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Token { get; set; }

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}
