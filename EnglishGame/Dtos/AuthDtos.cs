using EnglishGame.Models;

namespace EnglishGame.Dtos;

public record RegisterRequest(string FullName, string Email, string Password, CefrLevel Level);
public class LoginRequest
{
    public string Identifier { get; set; } = ""; // Email hoặc Họ tên
    public string Password { get; set; } = "";
}



public record AuthResponse(
    Guid UserId,
    string FullName,
    string Email,
    UserRole Role,
    CefrLevel Level,
    string Token
);


public class ForgotPasswordRequest
{
    public string Email { get; set; } = "";
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

