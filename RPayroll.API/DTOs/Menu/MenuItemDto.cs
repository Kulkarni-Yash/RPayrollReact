namespace RPayroll.API.DTOs.Menu;

public class MenuItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
    public string? FormLayoutKey { get; set; }
    public int VisibilityScope { get; set; }
    public int? MaxHierarchyLevelAllowed { get; set; }
    public int? OwnerUserId { get; set; }
}
