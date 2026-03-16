using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.API.DTOs.AIClassBuilder;
using RPayroll.Infrastructure.Interfaces.Services;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/ai-class-builder")]
[Authorize(Roles = "Admin")]
public class AIClassBuilderController : ControllerBase
{
    private readonly IAIClassBuilderService _aiClassBuilderService;

    public AIClassBuilderController(IAIClassBuilderService aiClassBuilderService)
    {
        _aiClassBuilderService = aiClassBuilderService;
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
}
