using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.API.DTOs.AIClassBuilder;
using RPayroll.Infrastructure.Interfaces.Services;
using RPayroll.Infrastructure.Interfaces.Services.Models;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/ai-class-builder")]
[Authorize(Roles = "Admin")]
public class AIClassBuilderController : ControllerBase
{
    private readonly IAIClassBuilderService _aiClassBuilderService;
    private readonly IAIClassBuilderAuditService _aiClassBuilderAuditService;

    public AIClassBuilderController(
        IAIClassBuilderService aiClassBuilderService,
        IAIClassBuilderAuditService aiClassBuilderAuditService)
    {
        _aiClassBuilderService = aiClassBuilderService;
        _aiClassBuilderAuditService = aiClassBuilderAuditService;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AIClassBuilderChatRequestDto dto, CancellationToken cancellationToken)
    {
        if (dto.Messages == null || dto.Messages.Count == 0)
        {
            return BadRequest(new { message = "At least one chat message is required." });
        }

        var result = await _aiClassBuilderService.ChatAsync(
            dto.Messages.Select(message => (message.Role, message.Content)),
            cancellationToken);

        return Ok(new AIClassBuilderChatResponseDto
        {
            Content = result.Content,
            Model = result.Model,
            UsedFiles = result.UsedFiles.ToList()
        });
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLog(CancellationToken cancellationToken)
    {
        var log = await _aiClassBuilderAuditService.GetLogAsync(cancellationToken);
        return Ok(new AIClassBuilderAuditLogDto
        {
            ProjectName = log.ProjectName,
            UpdatedAt = log.UpdatedAt,
            Entries = log.Entries.Select(MapEntry).ToList()
        });
    }

    [HttpPost("audit")]
    public async Task<IActionResult> AppendAuditLog([FromBody] AIClassBuilderAuditRequestDto dto, CancellationToken cancellationToken)
    {
        if (dto.Files == null || dto.Files.Count == 0)
        {
            return BadRequest(new { message = "At least one changed file is required." });
        }

        var entry = await _aiClassBuilderAuditService.AppendAsync(
            new AIClassBuilderAuditRequestContract
            {
                ModelName = dto.ModelName,
                Prompt = dto.Prompt,
                Summary = dto.Summary,
                Files = dto.Files.Select(file => new AIClassBuilderAuditFileChangeContract
                {
                    FileName = file.FileName,
                    FilePath = file.FilePath,
                    ChangeType = file.ChangeType,
                    TotalLines = file.TotalLines,
                    LinesChangedCount = file.LinesChangedCount,
                    ChangedLineNumbers = file.ChangedLineNumbers
                }).ToList()
            },
            cancellationToken);

        return Ok(MapEntry(entry));
    }

    private static AIClassBuilderAuditEntryDto MapEntry(AIClassBuilderAuditEntryContract entry)
    {
        return new AIClassBuilderAuditEntryDto
        {
            AuditId = entry.AuditId,
            ProjectName = entry.ProjectName,
            BranchName = entry.BranchName,
            ModelName = entry.ModelName,
            Prompt = entry.Prompt,
            Summary = entry.Summary,
            Timestamp = entry.Timestamp,
            Files = entry.Files.Select(file => new AIClassBuilderAuditFileChangeDto
            {
                FileName = file.FileName,
                FilePath = file.FilePath,
                ChangeType = file.ChangeType,
                TotalLines = file.TotalLines,
                LinesChangedCount = file.LinesChangedCount,
                ChangedLineNumbers = file.ChangedLineNumbers
            }).ToList()
        };
    }
}
