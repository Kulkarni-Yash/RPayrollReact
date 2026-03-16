namespace RPayroll.Infrastructure.Interfaces.Services.Models;

public class AIClassBuilderAuditEntryContract
{
    public string AuditId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public List<AIClassBuilderAuditFileChangeContract> Files { get; set; } = new();
}
