namespace RPayroll.Domain.DTOs.User;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int? EmployeeId { get; set; }
    public int Status { get; set; }
}
