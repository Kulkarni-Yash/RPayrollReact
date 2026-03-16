namespace RPayroll.API.DTOs.FormLayout;

public class FormLayoutDto
{
    public string? Key { get; set; }
    public string? MenuKey { get; set; }
    public string? Name { get; set; }
    public string? TargetLayoutKey { get; set; }
    public string SaveMode { get; set; } = "update";
    public int? OwnerUserId { get; set; }
    public int VisibilityScope { get; set; }
    public int? MaxHierarchyLevelAllowed { get; set; }
    public string Entity { get; set; } = string.Empty;
    public string? EntityLabel { get; set; }
    public string GridSize { get; set; } = "3x4";
    public string? SubmitUrl { get; set; }
    public string? SubmitMethod { get; set; }
    public List<FormLayoutItemDto> Layout { get; set; } = new();
    public List<FormLayoutTabDto> Tabs { get; set; } = new();
}
