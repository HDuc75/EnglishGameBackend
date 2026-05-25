using BCrypt.Net;
using EnglishGame.Data;
using EnglishGame.Dtos;
using EnglishGame.Models;
using EnglishGame.Services;
using Microsoft.EntityFrameworkCore;

namespace EnglishGame.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly TokenService _token;

    public AuthService(AppDbContext db, TokenService token)
    {
        _db = db;
        _token = token;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Email already exists.");

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = UserRole.Learner,
            Level = req.Level
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _token.CreateToken(user);
        return new AuthResponse(user.Id, user.FullName, user.Email, user.Role, user.Level, token);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var identifier = (req.Identifier ?? "").Trim();

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(req.Password))
            throw new InvalidOperationException("Invalid email or password.");

        User? user = null;

        // Nếu người dùng nhập email (có '@')
        if (identifier.Contains("@"))
        {
            var email = identifier.ToLowerInvariant();
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
        else
        {
            // Người dùng nhập họ tên
            var name = identifier.ToLower(); // lowercase để so sánh không phân biệt hoa/thường
            user = await _db.Users.FirstOrDefaultAsync(u => u.FullName.ToLower() == name);
        }

        if (user == null)
            throw new InvalidOperationException("Invalid email or password.");

        try
        {
            if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                throw new InvalidOperationException("Invalid email or password.");
        }
        catch
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        var token = _token.CreateToken(user);
        return new AuthResponse(user.Id, user.FullName, user.Email, user.Role, user.Level, token);
    }

    public async Task<object> ForgotPasswordAsync(ForgotPasswordRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            // tránh lộ email tồn tại hay không
            return new { message = "If the email exists, a reset token has been generated." };
        }

        var token = Guid.NewGuid().ToString("N"); // dev token
        user.ResetToken = token;
        user.ResetTokenExpiresUtc = DateTime.UtcNow.AddMinutes(15);

        await _db.SaveChangesAsync();

        // DEV: trả token ra luôn để test (đồ án demo)
        return new { message = "Reset token generated (DEV).", token, expiresUtc = user.ResetTokenExpiresUtc };
    }

    public async Task<object> ResetPasswordAsync(ResetPasswordRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var token = (req.Token ?? "").Trim();
        var newPassword = req.NewPassword ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            throw new InvalidOperationException("Email, token and newPassword are required.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
                   ?? throw new InvalidOperationException("Invalid token.");

        if (user.ResetToken == null || user.ResetTokenExpiresUtc == null)
            throw new InvalidOperationException("Invalid token.");

        if (!string.Equals(user.ResetToken, token, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid token.");

        if (user.ResetTokenExpiresUtc < DateTime.UtcNow)
            throw new InvalidOperationException("Token expired.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordUpdatedAtUtc = DateTime.UtcNow;

        user.ResetToken = null;
        user.ResetTokenExpiresUtc = null;

        await _db.SaveChangesAsync();

        return new { message = "Password reset successfully." };
    }


}
