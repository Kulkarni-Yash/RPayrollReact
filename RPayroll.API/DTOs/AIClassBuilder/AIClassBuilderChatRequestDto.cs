namespace RPayroll.API.DTOs.AIClassBuilder;

public class AIClassBuilderChatRequestDto
{
    public List<AIClassBuilderChatMessageDto> Messages { get; set; } = new();
}
