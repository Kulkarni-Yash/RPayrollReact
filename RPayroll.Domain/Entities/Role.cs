using RPayroll.Domain.Common;

namespace RPayroll.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int HierarchyLevel { get; set; }
    public List<User> Users { get; set; } = new();
}
