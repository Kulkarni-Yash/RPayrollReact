using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using RPayroll.Infrastructure.Interfaces.Services;

namespace RPayroll.Repositories.Services;

public class AIClassBuilderService : IAIClassBuilderService
{
    private readonly HttpClient _httpClient;
    private readonly IWebHostEnvironment _environment;
    private readonly string _ollamaBaseUrl;
    private readonly string _modelName;
    private readonly string _repoRoot;

    public AIClassBuilderService(HttpClient httpClient, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _environment = environment;
        _ollamaBaseUrl = (configuration["OLLAMA_BASE_URL"] ?? configuration["Ollama:BaseUrl"] ?? "http://127.0.0.1:11434").TrimEnd('/');
        _modelName = configuration["OLLAMA_MODEL"] ?? configuration["Ollama:Model"] ?? "deepseek-coder:6.7b";
        _repoRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, ".."));
    }

    public async Task<(string Content, string Model, IReadOnlyList<string> UsedFiles)> ChatAsync(
        IEnumerable<(string Role, string Content)> messages,
        CancellationToken cancellationToken = default)
    {
        var normalizedMessages = messages
            .Where(message => !string.IsNullOrWhiteSpace(message.Content))
            .Select(message => (Role: NormalizeRole(message.Role), Content: message.Content.Trim()))
            .ToList();

        if (normalizedMessages.Count == 0)
        {
            throw new InvalidOperationException("No valid chat messages were provided.");
        }

        var latestUserPrompt = normalizedMessages.LastOrDefault(message => message.Role == "user").Content ?? string.Empty;
        var context = BuildProjectContext(latestUserPrompt);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_ollamaBaseUrl}/api/chat")
        {
            Content = JsonContent.Create(new
            {
                model = _modelName,
                stream = false,
                messages = BuildOllamaMessages(normalizedMessages, context.ContextText)
            })
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var failureText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(failureText)
                    ? $"Ollama request failed ({(int)response.StatusCode})."
                    : failureText);
        }

        var payload = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken);
        var content = payload?.Message?.Content?.Trim();

        return (
            string.IsNullOrWhiteSpace(content) ? "I could not generate a response." : content,
            _modelName,
            context.UsedFiles);
    }

    private List<object> BuildOllamaMessages(
        IReadOnlyList<(string Role, string Content)> messages,
        string contextText)
    {
        return
        [
            new
            {
                role = "system",
                content =
                    "You are an AI class builder embedded in the RPayroll project. " +
                    "Help generate classes, DTOs, interfaces, repositories, services, controller contracts, and architecture-aligned code. " +
                    "Follow the existing project structure: Domain for entities/enums, Infrastructure for interfaces, Repositories for implementations, API for controllers/DTOs, React UI for frontend pages. " +
                    "When the user asks for a class, produce code that fits this structure and naming style. " +
                    "Use the provided project context and matching files when relevant."
            },
            new
            {
                role = "system",
                content = contextText
            },
            ..messages.Select(message => new
            {
                role = message.Role,
                content = message.Content
            })
        ];
    }

    private (string ContextText, IReadOnlyList<string> UsedFiles) BuildProjectContext(string prompt)
    {
        var importantFiles = FindRelevantFiles(prompt);
        var builder = new StringBuilder();
        builder.AppendLine("Project Context");
        builder.AppendLine("===============");
        builder.AppendLine("Repository: RPayrollReact");
        builder.AppendLine("Solution projects:");
        builder.AppendLine("- RPayroll.API: controllers and API DTOs");
        builder.AppendLine("- RPayroll.Domain: entities and enums");
        builder.AppendLine("- RPayroll.Infrastructure: interfaces/contracts");
        builder.AppendLine("- RPayroll.Repositories: service/repository implementations");
        builder.AppendLine("- rpayroll-ui: React + TypeScript frontend");
        builder.AppendLine();
        builder.AppendLine("Current architectural conventions:");
        builder.AppendLine("- Dynamic forms are stored as backend layout JSON.");
        builder.AppendLine("- Role-based access uses Role.HierarchyLevel (lower number = higher privilege).");
        builder.AppendLine("- AI builder outputs should align with the existing folder structure and naming.");

        if (importantFiles.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Relevant file excerpts:");

            foreach (var relativePath in importantFiles)
            {
                var absolutePath = Path.Combine(_repoRoot, relativePath);
                if (!File.Exists(absolutePath))
                {
                    continue;
                }

                var fileContent = File.ReadAllText(absolutePath);
                var truncated = fileContent.Length > 4000 ? fileContent[..4000] : fileContent;
                builder.AppendLine($"--- FILE: {relativePath} ---");
                builder.AppendLine(truncated);
            }
        }

        return (builder.ToString(), importantFiles);
    }

    private List<string> FindRelevantFiles(string prompt)
    {
        var seedFiles = new[]
        {
            "RPayroll.Domain/Entities/Role.cs",
            "RPayroll.Domain/Entities/User.cs",
            "RPayroll.Domain/Entities/Employee.cs",
            "RPayroll.API/Program.cs",
            "RPayroll.Repositories/Services/FormLayoutService.cs"
        }.ToList();

        var promptTokens = Regex.Matches(prompt, "[A-Za-z][A-Za-z0-9_-]{3,}")
            .Select(match => match.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var candidateFiles = Directory
            .EnumerateFiles(_repoRoot, "*.*", SearchOption.AllDirectories)
            .Where(path =>
                path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(_repoRoot, path).Replace('\\', '/'))
            .Where(path => !path.Contains("/bin/") && !path.Contains("/obj/") && !path.Contains("/node_modules/"))
            .ToList();

        foreach (var token in promptTokens)
        {
            foreach (var candidate in candidateFiles)
            {
                if (seedFiles.Count >= 8)
                {
                    break;
                }

                if (candidate.Contains(token, StringComparison.OrdinalIgnoreCase) &&
                    !seedFiles.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                {
                    seedFiles.Add(candidate);
                }
            }
        }

        return seedFiles.Take(8).ToList();
    }

    private static string NormalizeRole(string role)
    {
        return string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase)
            ? "assistant"
            : "user";
    }

    private sealed class OllamaChatResponse
    {
        public OllamaMessage? Message { get; set; }
    }

    private sealed class OllamaMessage
    {
        public string? Content { get; set; }
    }
}
