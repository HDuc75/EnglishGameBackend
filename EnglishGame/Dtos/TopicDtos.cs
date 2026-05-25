namespace EnglishGame.Dtos;

public record TopicResponse(Guid Id, string Name, string? Description);
public record CreateTopicRequest(string Name, string? Description);
