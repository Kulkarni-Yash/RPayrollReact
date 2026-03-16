using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using RPayroll.Infrastructure.Interfaces.Services;
using RPayroll.Infrastructure.Interfaces.Services.Models;

namespace RPayroll.Repositories.Services;

public class AIClassBuilderAuditService : IAIClassBuilderAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _repoRoot;
    private readonly string _projectName;
    private readonly string _logPath;

    public AIClassBuilderAuditService(IWebHostEnvironment environment)
    {
        _repoRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, ".."));
        _projectName = new DirectoryInfo(_repoRoot).Name;
        _logPath = Path.Combine(_repoRoot, "docs", "ollama-change-log.json");
    }

    public async Task<AIClassBuilderAuditLogContract> GetLogAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLogExistsAsync(cancellationToken);
        await using var stream = File.OpenRead(_logPath);
        var log = await JsonSerializer.DeserializeAsync<AIClassBuilderAuditLogContract>(stream, cancellationToken: cancellationToken);
        return log ?? CreateEmptyLog();
    }

    public async Task<AIClassBuilderAuditEntryContract> AppendAsync(AIClassBuilderAuditRequestContract request, CancellationToken cancellationToken = default)
    {
        var log = await GetLogAsync(cancellationToken);
        var entry = new AIClassBuilderAuditEntryContract
        {
            AuditId = $"audit_{DateTimeOffset.Now:yyyyMMddHHmmssfff}",
            ProjectName = _projectName,
            BranchName = await ResolveBranchNameAsync(cancellationToken),
            ModelName = string.IsNullOrWhiteSpace(request.ModelName) ? "unknown" : request.ModelName.Trim(),
            Prompt = request.Prompt?.Trim() ?? string.Empty,
            Summary = request.Summary?.Trim() ?? string.Empty,
            Timestamp = DateTimeOffset.Now.ToString("O"),
            Files = request.Files.Select(BuildFileEntry).ToList()
        };

        log.ProjectName = _projectName;
        log.UpdatedAt = entry.Timestamp;
        log.Entries.Insert(0, entry);

        await WriteLogAsync(log, cancellationToken);
        return entry;
    }

    private AIClassBuilderAuditFileChangeContract BuildFileEntry(AIClassBuilderAuditFileChangeContract request)
    {
        var relativePath = request.FilePath.Replace('\\', '/').Trim();
        var absolutePath = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(_repoRoot, relativePath);

        var totalLines = File.Exists(absolutePath) ? File.ReadAllLines(absolutePath).Length : 0;
        var normalizedLineNumbers = request.ChangedLineNumbers
            .Where(line => line > 0)
            .Distinct()
            .OrderBy(line => line)
            .ToList();

        return new AIClassBuilderAuditFileChangeContract
        {
            FileName = string.IsNullOrWhiteSpace(request.FileName)
                ? Path.GetFileName(absolutePath)
                : request.FileName,
            FilePath = absolutePath,
            ChangeType = string.IsNullOrWhiteSpace(request.ChangeType) ? "modified" : request.ChangeType,
            TotalLines = totalLines,
            LinesChangedCount = normalizedLineNumbers.Count > 0
                ? normalizedLineNumbers.Count
                : request.LinesChangedCount,
            ChangedLineNumbers = normalizedLineNumbers
        };
    }

    private async Task EnsureLogExistsAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(_logPath))
        {
            return;
        }

        await WriteLogAsync(CreateEmptyLog(), cancellationToken);
    }

    private async Task WriteLogAsync(AIClassBuilderAuditLogContract log, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_logPath);
        await JsonSerializer.SerializeAsync(stream, log, JsonOptions, cancellationToken);
    }

    private AIClassBuilderAuditLogContract CreateEmptyLog()
    {
        return new AIClassBuilderAuditLogContract
        {
            ProjectName = _projectName,
            UpdatedAt = DateTimeOffset.Now.ToString("O"),
            Entries = new List<AIClassBuilderAuditEntryContract>()
        };
    }

    private async Task<string> ResolveBranchNameAsync(CancellationToken cancellationToken)
    {
        var headPath = Path.Combine(_repoRoot, ".git", "HEAD");
        if (!File.Exists(headPath))
        {
            return "unknown";
        }

        var head = (await File.ReadAllTextAsync(headPath, cancellationToken)).Trim();
        const string prefix = "ref: refs/heads/";
        return head.StartsWith(prefix, StringComparison.Ordinal)
            ? head[prefix.Length..]
            : head;
    }
}
