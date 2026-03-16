namespace RPayroll.Infrastructure.Interfaces.Services.Models;

public class AIClassBuilderAuditLogContract
{
    public string ProjectName { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public List<AIClassBuilderAuditEntryContract> Entries { get; set; } = new();
}
