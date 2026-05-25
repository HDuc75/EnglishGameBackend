using EnglishGame.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishGame.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public LeaderboardController(AppDbContext db)
    {
        _db = db;
    }

    public record LeaderboardRowDto(
        int Rank,
        Guid UserId,
        string FullName,
        int Role,
        int Level,
        int Xp,
        int TotalScore,
        int CompletedSessions
    );

    // GET /api/leaderboard?top=50&level=0|1
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<LeaderboardRowDto>>> Get([FromQuery] int top = 50, [FromQuery] int? level = null)
    {
        top = Math.Clamp(top, 5, 200);

        // ✅ 1) Aggregate sessions (only finished)
        var sessionStats = await _db.GameSessions
            .AsNoTracking()
            .Where(s => s.FinishedAtUtc != null)
            .GroupBy(s => s.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                CompletedSessions = g.Count(),
                TotalScore = g.Sum(x => x.TotalScore)
            })
            .ToListAsync();

        var statsMap = sessionStats.ToDictionary(x => x.UserId, x => x);

        // ✅ 2) Load users (optional filter by level)
        var usersQuery = _db.Users.AsNoTracking();

        if (level is 0 or 1)
        {
            // User.Level is enum in db, but comparing int works
            usersQuery = usersQuery.Where(u => (int)u.Level == level.Value);
        }

        var users = await usersQuery
            .Select(u => new
            {
                u.Id,
                u.FullName,
                Role = (int)u.Role,
                Level = (int)u.Level,
                u.Xp
            })
            .ToListAsync();

        // ✅ 3) Merge in-memory
        var merged = users.Select(u =>
        {
            var has = statsMap.TryGetValue(u.Id, out var st);
            return new
            {
                u.Id,
                u.FullName,
                u.Role,
                u.Level,
                u.Xp,
                TotalScore = has ? st!.TotalScore : 0,
                CompletedSessions = has ? st!.CompletedSessions : 0
            };
        });

        // ✅ 4) Sort + rank + take top
        var ranked = merged
            .OrderByDescending(x => x.TotalScore)
            .ThenByDescending(x => x.CompletedSessions)
            .ThenByDescending(x => x.Xp)
            .Take(top)
            .Select((x, idx) => new LeaderboardRowDto(
                Rank: idx + 1,
                UserId: x.Id,
                FullName: x.FullName,
                Role: x.Role,
                Level: x.Level,
                Xp: x.Xp,
                TotalScore: x.TotalScore,
                CompletedSessions: x.CompletedSessions
            ))
            .ToList();

        return Ok(ranked);
    }
}