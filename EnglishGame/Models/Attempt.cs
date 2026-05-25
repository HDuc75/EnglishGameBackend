using System.ComponentModel.DataAnnotations;

namespace EnglishGame.Models;

public class Attempt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid GameSessionId { get; set; }
    public GameSession? GameSession { get; set; }

    public int QuestionNo { get; set; }

    [MaxLength(2000)]
    public string UserAnswer { get; set; } = "";

    public bool IsCorrect { get; set; }
    public int Score { get; set; }

    [MaxLength(2000)]
    public string Explanation { get; set; } = "";
}
