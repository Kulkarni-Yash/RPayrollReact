namespace RPayroll.API.DTOs.AIClassBuilder;

public class AIClassBuilderChatResponseDto
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public List<string> UsedFiles { get; set; } = new();
}
