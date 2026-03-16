using RPayroll.Domain.Common;
using RPayroll.Domain.Enums;

namespace RPayroll.Domain.Entities;

public class AppMenuItem : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsSystem { get; set; }
    public string? FormLayoutKey { get; set; }
    public int? OwnerUserId { get; set; }
    public VisibilityScope VisibilityScope { get; set; } = VisibilityScope.AllUsers;
    public int? MaxHierarchyLevelAllowed { get; set; }
}
