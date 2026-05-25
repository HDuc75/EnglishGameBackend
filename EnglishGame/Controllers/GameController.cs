using EnglishGame.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishGame.Api.Controllers;

[ApiController]
[Route("api/game")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly Services.GameService _game;

    public GameController(Services.GameService game) => _game = game;

    [HttpPost("start")]
    public async Task<ActionResult<StartGameResponse>> Start([FromBody] StartGameRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _game.StartAsync(User, req, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Internal server error.",
                detail = ex.Message
            });
        }
    }

    [HttpPost("submit")]
    public async Task<ActionResult<SubmitGameResponse>> Submit([FromBody] SubmitGameRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _game.SubmitAsync(User, req, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Internal server error.",
                detail = ex.Message
            });
        }
    }

    [HttpPost("submit-writing")]
    public async Task<ActionResult<SubmitWritingResponse>> SubmitWriting([FromBody] SubmitWritingRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _game.SubmitWritingAsync(User, req, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Internal server error.",
                detail = ex.Message
            });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<object>> History(CancellationToken ct)
    {
        try
        {
            return Ok(await _game.GetHistoryAsync(User, ct));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Internal server error.",
                detail = ex.Message
            });
        }
    }

    // ✅ NEW: Review
    [HttpGet("review/{sessionId:guid}")]
    public async Task<ActionResult<object>> Review([FromRoute] Guid sessionId, CancellationToken ct)
    {
        try
        {
            return Ok(await _game.GetReviewAsync(User, sessionId, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Internal server error.",
                detail = ex.Message
            });
        }
    }

    // ✅ NEW: Get Session (resume)
    [HttpGet("session/{sessionId:guid}")]
    public async Task<ActionResult<object>> Session([FromRoute] Guid sessionId, CancellationToken ct)
    {
        try
        {
            return Ok(await _game.GetSessionAsync(User, sessionId, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Internal server error.",
                detail = ex.Message
            });
        }
    }

    [HttpPost("finish-exam/{sessionId:guid}")]
    public async Task<ActionResult<FinishExamResponse>> FinishExam([FromRoute] Guid sessionId, CancellationToken ct)
    {
        try
        {
            return Ok(await _game.FinishExamAsync(User, sessionId, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error.", detail = ex.Message });
        }
    }

    [HttpPost("submit-speaking")]
    public async Task<ActionResult<SubmitSpeakingResponse>> SubmitSpeaking([FromBody] SubmitSpeakingRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _game.SubmitSpeakingAsync(User, req, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error.", detail = ex.Message });
        }
    }
}
