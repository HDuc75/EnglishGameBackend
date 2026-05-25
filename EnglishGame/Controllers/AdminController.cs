using EnglishGame.Data;
using EnglishGame.Dtos;
using EnglishGame.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishGame.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    // ---------------- TOPICS ----------------
    [HttpPost("topics")]
    public async Task<IActionResult> CreateTopic(CreateTopicRequest req)
    {
        var t = new Topic { Name = req.Name.Trim(), Description = req.Description };
        _db.Topics.Add(t);
        await _db.SaveChangesAsync();
        return Ok(new { t.Id });
    }

    [HttpDelete("topics/{id:guid}")]
    public async Task<IActionResult> DeleteTopic(Guid id)
    {
        var topic = await _db.Topics.FirstOrDefaultAsync(t => t.Id == id);
        if (topic == null) return NotFound();

        topic.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Topic deleted" });
    }

    // ---------------- AI CONTENT ----------------
    [HttpGet("ai-content")]
    public async Task<ActionResult<List<PendingAiContentResponse>>> ListAiContent([FromQuery] ReviewStatus status = ReviewStatus.Pending)
    {
        var list = await _db.AiContents
            .Include(x => x.Topic)
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new PendingAiContentResponse(
                x.Id, x.Type, x.Level, x.TopicId, x.Topic!.Name, x.Status, x.CreatedAtUtc
            ))
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost("ai-content/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var item = await _db.AiContents.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();

        item.Status = ReviewStatus.Approved;
        item.ReviewedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Approved" });
    }

    [HttpPost("ai-content/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var item = await _db.AiContents.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();

        item.Status = ReviewStatus.Rejected;
        item.ReviewedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Rejected" });
    }

    // ---------------- USERS (NEW) ----------------

    // GET /api/admin/users?role=learner|admin|all&q=...
    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> ListUsers([FromQuery] string role = "learner", [FromQuery] string? q = null)
    {
        var query = _db.Users.AsQueryable();

        role = (role ?? "learner").Trim().ToLowerInvariant();
        if (role == "admin") query = query.Where(u => u.Role == UserRole.Admin);
        else if (role == "learner") query = query.Where(u => u.Role == UserRole.Learner);
        // all => không filter

        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.Email.ToLower().Contains(s) ||
                u.FullName.ToLower().Contains(s)
            );
        }

        var list = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new AdminUserDto(
                u.Id, u.FullName, u.Email, u.Role, u.Level, u.Xp, u.CreatedAtUtc
            ))
            .ToListAsync();

        return Ok(list);
    }

    // PUT /api/admin/users/{id}/role  body: { "role": 0|1 }  (0=Learner, 1=Admin)
    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, UpdateUserRoleRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.Role = req.Role;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Role updated" });
    }

    // PUT /api/admin/users/{id}/level body: { "level": 0|1 } (0=A2, 1=B1)
    [HttpPut("users/{id:guid}/level")]
    public async Task<IActionResult> UpdateUserLevel(Guid id, UpdateUserLevelRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.Level = req.Level;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Level updated" });
    }

    // POST /api/admin/users/{id}/reset-password body: { "newPassword": "..." }
    [HttpPost("users/{id:guid}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(Guid id, ResetUserPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

        // nếu bạn có cột này thì giữ, không có thì xoá 2 dòng dưới
        user.PasswordUpdatedAtUtc = DateTime.UtcNow;
        user.ResetToken = null;
        user.ResetTokenExpiresUtc = null;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Password reset" });
    }

    // DELETE /api/admin/users/{id}
    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}

// ---------------- DTOs (NEW) ----------------
public record AdminUserDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    CefrLevel Level,
    int Xp,
    DateTime CreatedAtUtc
);

public class UpdateUserRoleRequest
{
    public UserRole Role { get; set; }
}

public class UpdateUserLevelRequest
{
    public CefrLevel Level { get; set; }
}

public class ResetUserPasswordRequest
{
    public string NewPassword { get; set; } = "";
}
