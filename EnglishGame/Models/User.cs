using EnglishGame.Models;
using System.ComponentModel.DataAnnotations;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(80)]
    public string FullName { get; set; } = "";

    [Required, MaxLength(120)]
    public string Email { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    public UserRole Role { get; set; } = UserRole.Learner;
    public CefrLevel Level { get; set; } = CefrLevel.A2;

    public int Xp { get; set; } = 0;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiresUtc { get; set; }
    public DateTime? PasswordUpdatedAtUtc { get; set; }

}
