namespace RPayroll.API.DTOs.AIClassBuilder;

public class AIClassBuilderAuditRequestDto
{
    public string ModelName { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<AIClassBuilderAuditFileChangeDto> Files { get; set; } = new();
}
