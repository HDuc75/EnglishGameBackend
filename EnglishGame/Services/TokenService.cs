// TokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EnglishGame.Models;
using Microsoft.IdentityModel.Tokens;

namespace EnglishGame.Services;

public class TokenService
{
    private readonly IConfiguration _cfg;

    public TokenService(IConfiguration cfg) => _cfg = cfg;

    public string CreateToken(User user)
    {
        var key = _cfg["Jwt:Key"]!;
        var issuer = _cfg["Jwt:Issuer"]!;
        var audience = _cfg["Jwt:Audience"]!;
        var minutes = int.Parse(_cfg["Jwt:ExpireMinutes"] ?? "120");

        // ✅ IMPORTANT:
        // - Put role into ClaimTypes.Role so [Authorize(Roles="Admin")] works out of the box
        // - Keep "role" as string too (optional; useful for FE/debug)
        var roleName = user.Role == UserRole.Admin ? "Admin" : "Learner";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.FullName),
            new("level", user.Level.ToString()),

            // ✅ Standard role claim
            new(ClaimTypes.Role, roleName),

            // ✅ Optional: keep old claim too
            new("role", roleName),
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
