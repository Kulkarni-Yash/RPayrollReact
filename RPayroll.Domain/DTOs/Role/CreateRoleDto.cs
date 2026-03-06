namespace RPayroll.Domain.DTOs.Role;

public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public int HierarchyLevel { get; set; }
}
