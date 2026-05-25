using EnglishGame.Models;

public class GameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public GameMode Mode { get; set; }
    public CefrLevel Level { get; set; }

    public Guid TopicId { get; set; }
    public Topic? Topic { get; set; }

    public Guid AiContentId { get; set; }
    public AiContent? AiContent { get; set; }

    public int TotalScore { get; set; } = 0;

    // ✅ dùng DateTimeOffset để giữ timezone/offset chuẩn
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAtUtc { get; set; }
}
