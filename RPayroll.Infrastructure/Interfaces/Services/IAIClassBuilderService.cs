namespace RPayroll.Infrastructure.Interfaces.Services;

public interface IAIClassBuilderService
{
    Task<(string Content, string Model, IReadOnlyList<string> UsedFiles)> ChatAsync(
        IEnumerable<(string Role, string Content)> messages,
        CancellationToken cancellationToken = default);
}
