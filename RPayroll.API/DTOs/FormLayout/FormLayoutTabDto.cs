namespace RPayroll.API.DTOs.FormLayout;

public class FormLayoutTabDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GridSize { get; set; } = "3x4";
    public List<FormLayoutItemDto> Layout { get; set; } = new();
}
