using EnglishGame.Models;

namespace EnglishGame.Dtos;

public record PendingAiContentResponse(
    Guid Id,
    AiContentType Type,
    CefrLevel Level,
    Guid TopicId,
    string TopicName,
    ReviewStatus Status,
    DateTimeOffset CreatedAtUtc
);
