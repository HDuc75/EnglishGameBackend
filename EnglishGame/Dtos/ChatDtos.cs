using System.ComponentModel.DataAnnotations;

namespace EnglishGame.Dtos;

public record ChatMessageDto(
    [Required] string Role,   // "user" | "assistant"
    [Required] string Content
);

public record ChatRequest(
    string? Topic,
    string? Level,
    [Required] List<ChatMessageDto> Messages
);

public record ChatResponse(string Reply);