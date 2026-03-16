namespace RPayroll.Infrastructure.Interfaces.Services.Models;

public class AIClassBuilderAuditFileChangeContract
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ChangeType { get; set; } = "modified";
    public int TotalLines { get; set; }
    public int LinesChangedCount { get; set; }
    public List<int> ChangedLineNumbers { get; set; } = new();
}
