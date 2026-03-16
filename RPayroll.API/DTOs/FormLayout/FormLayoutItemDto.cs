namespace RPayroll.API.DTOs.FormLayout;

public class FormLayoutItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Variable { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
}
