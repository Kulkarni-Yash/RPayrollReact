namespace RPayroll.Domain.DTOs.User;

public class UpdateUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public int RoleId { get; set; }
    public int? EmployeeId { get; set; }
    public int Status { get; set; }
}
