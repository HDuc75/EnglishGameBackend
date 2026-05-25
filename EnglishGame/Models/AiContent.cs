using EnglishGame.Models;
using System.ComponentModel.DataAnnotations;

public class AiContent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public AiContentType Type { get; set; }
    public CefrLevel Level { get; set; }

    public Guid TopicId { get; set; }
    public Topic? Topic { get; set; }

    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    [MaxLength(4000)]
    public string RawPrompt { get; set; } = "";

    public string RawResponse { get; set; } = "";

    public string ContentJson { get; set; } = "";

    public Guid CreatedByUserId { get; set; }

    // ✅ dùng DateTimeOffset để API trả về có +00:00 (JS sẽ tự đổi giờ local)
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
}
