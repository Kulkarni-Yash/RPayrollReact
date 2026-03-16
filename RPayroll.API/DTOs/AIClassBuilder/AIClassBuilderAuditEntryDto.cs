namespace RPayroll.API.DTOs.AIClassBuilder;

public class AIClassBuilderAuditEntryDto
{
    public string AuditId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public List<AIClassBuilderAuditFileChangeDto> Files { get; set; } = new();
}
