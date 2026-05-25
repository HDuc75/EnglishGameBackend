using System.ComponentModel.DataAnnotations;
using EnglishGame.Models;

namespace EnglishGame.Dtos;

public record AdminUserDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    CefrLevel Level,
    int Xp,
    DateTime CreatedAtUtc
);

public record AdminSetUserRoleRequest(
    [Required] UserRole Role
);

public record AdminSetUserLevelRequest(
    [Required] CefrLevel Level
);

public record AdminResetPasswordRequest(
    [Required, MinLength(6)] string NewPassword
);
