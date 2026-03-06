namespace RPayroll.Domain.DTOs.Menu;

public class MenuItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Order { get; set; }
}
