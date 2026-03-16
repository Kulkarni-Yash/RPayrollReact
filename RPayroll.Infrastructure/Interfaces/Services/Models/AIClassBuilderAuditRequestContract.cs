namespace RPayroll.Infrastructure.Interfaces.Services.Models;

public class AIClassBuilderAuditRequestContract
{
    public string ModelName { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<AIClassBuilderAuditFileChangeContract> Files { get; set; } = new();
}
