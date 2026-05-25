using System.ComponentModel.DataAnnotations;

namespace EnglishGame.Models;

public class Topic
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(80)]
    public string Name { get; set; } = "";

    [MaxLength(240)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
