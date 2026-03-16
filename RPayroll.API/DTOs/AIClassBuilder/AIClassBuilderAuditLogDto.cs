namespace RPayroll.API.DTOs.AIClassBuilder;

public class AIClassBuilderAuditLogDto
{
    public string ProjectName { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public List<AIClassBuilderAuditEntryDto> Entries { get; set; } = new();
}
