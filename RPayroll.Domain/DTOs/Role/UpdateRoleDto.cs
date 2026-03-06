namespace RPayroll.Domain.DTOs.Role;

public class UpdateRoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int HierarchyLevel { get; set; }
    public int Status { get; set; }
}
