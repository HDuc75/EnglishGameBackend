
using EnglishGame.Data;
using EnglishGame.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishGame.Controllers;

[ApiController]
[Route("api/topics")]
public class TopicsController : ControllerBase
{
    private readonly AppDbContext _db;
    public TopicsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TopicResponse>>> Get()
    {
        var topics = await _db.Topics
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new TopicResponse(t.Id, t.Name, t.Description))
            .ToListAsync();

        return Ok(topics);
    }
}
