using RPayroll.Domain.Common;
using RPayroll.Domain.Enums;

namespace RPayroll.Domain.Entities;

public class FormLayoutDefinition : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ModelKey { get; set; } = string.Empty;
    public string ModelLabel { get; set; } = string.Empty;
    public string GridSize { get; set; } = "3x4";
    public string SubmitUrl { get; set; } = string.Empty;
    public string SubmitMethod { get; set; } = "POST";
    public string LayoutJson { get; set; } = "[]";
    public string MenuKey { get; set; } = string.Empty;
    public int? OwnerUserId { get; set; }
    public VisibilityScope VisibilityScope { get; set; } = VisibilityScope.AllUsers;
    public int? MaxHierarchyLevelAllowed { get; set; }
}
