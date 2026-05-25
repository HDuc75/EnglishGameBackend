using EnglishGame.Dtos;
using EnglishGame.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishGame.Controllers;

[ApiController]
[Route("api/ai")]
public class AiChatController : ControllerBase
{
    private readonly IAiGenerator _ai;

    public AiChatController(IAiGenerator ai)
    {
        _ai = ai;
    }

    [Authorize]
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest req, CancellationToken ct)
    {
        var topic = string.IsNullOrWhiteSpace(req.Topic) ? "General English" : req.Topic!;
        var level = string.IsNullOrWhiteSpace(req.Level) ? "A2" : req.Level!;

        var messages = new List<(string role, string content)>
        {
            ("system",
             $"You are ChatGPT, an English tutor. Topic: {topic}. Level: {level}. " +
             "Be natural, conversational, and helpful. Correct mistakes gently.")
        };

        foreach (var m in req.Messages.TakeLast(20))
        {
            var role = (m.Role ?? "user").Trim().ToLowerInvariant();
            if (role != "user" && role != "assistant") role = "user";
            messages.Add((role, m.Content));
        }

        var reply = await _ai.ChatAsync(messages, ct);
        return Ok(new ChatResponse(reply));
    }

    [Authorize]
    [HttpGet("translate")]
    public async Task<ActionResult> Translate([FromQuery] string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return BadRequest(new { message = "Text is required" });
        if (text.Length > 200) return BadRequest(new { message = "Text too long for translation" });

        var messages = new List<(string role, string content)>
        {
            ("user", $"You are an English-Vietnamese dictionary. Translate the following English text/word to Vietnamese concisely and accurately. Provide ONLY the translation, nothing else.\n\nText to translate: \"{text}\"")
        };

        try 
        {
            var reply = await _ai.ChatAsync(messages, ct);
            return Ok(new { translation = reply.Trim() });
        }
        catch (Exception ex)
        {
            // Dump detailed error to translation field so it displays on the frontend without 500 masking
            return Ok(new { translation = "Lỗi Exception: " + ex.Message + " | " + ex.InnerException?.Message });
        }
    }
}