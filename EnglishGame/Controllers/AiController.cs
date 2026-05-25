
using EnglishGame.Data;
using EnglishGame.Models;
using EnglishGame.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EnglishGame.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize(Roles = "Admin")]
public class AiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAiGenerator _ai;

    public AiController(AppDbContext db, IAiGenerator ai)
    {
        _db = db;
        _ai = ai;
    }

    [HttpPost("generate/quiz")]
    public async Task<IActionResult> GenerateQuiz([FromQuery] Guid topicId, [FromQuery] CefrLevel level = CefrLevel.A2, [FromQuery] int count = 5, CancellationToken ct = default)
    {
        var topic = await _db.Topics.FirstOrDefaultAsync(t => t.Id == topicId, ct);
        if (topic == null) return NotFound(new { message = "Topic not found" });

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        var (prompt, raw, json) = await _ai.GenerateQuizAsync(topic.Name, level.ToString(), count, ct);

        var content = new AiContent
        {
            Type = AiContentType.Quiz,
            Level = level,
            TopicId = topic.Id,
            Status = ReviewStatus.Pending,
            RawPrompt = prompt,
            RawResponse = raw,
            ContentJson = json,
            CreatedByUserId = userId
        };

        _db.AiContents.Add(content);
        await _db.SaveChangesAsync(ct);

        return Ok(new { content.Id, content.Status });
    }
}
