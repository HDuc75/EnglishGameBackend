using System.ComponentModel.DataAnnotations;
using EnglishGame.Models;

namespace EnglishGame.Dtos;

// ✅ đổi sang record có property để gắn validation
public record StartGameRequest
{
    [Required]
    public GameMode Mode { get; init; }

    [Required]
    public Guid TopicId { get; init; }

    [Required]
    public CefrLevel Level { get; init; }

    // ✅ chặn 1–4 ngay từ request
    [Range(5, 50, ErrorMessage = "QuestionCount must be between 5 and 50.")]
    public int QuestionCount { get; init; } = 5;

    // ✅ NEW: bắt buộc tạo quiz bằng AI (bỏ qua cache/content có sẵn)
    public bool ForceAi { get; init; } = false;
}

public record StartGameResponse(
    Guid SessionId,
    Guid TopicId,
    string TopicName,
    CefrLevel Level,
    GameMode Mode,
    object Content
);

public record SubmitAnswerItem(int No, string Answer);

public record SubmitGameRequest(Guid SessionId, List<SubmitAnswerItem> Answers);

public record SubmitGameResponse(
    Guid SessionId,
    int TotalScore,
    List<PerQuestionResult> Results
);

public record PerQuestionResult(int No, bool IsCorrect, int Score, string Explanation);

public record SubmitWritingItem(string TaskId, string Content);

public record SubmitWritingRequest(Guid SessionId, List<SubmitWritingItem> Answers);

public record WritingFeedback(
    string TaskId, 
    int Score, 
    string GrammarFeedback, 
    string VocabularyFeedback, 
    string CoherenceFeedback,
    string TaskFulfillmentFeedback,
    string OverallComment
);

public record SubmitWritingResponse(
    Guid SessionId,
    int TotalScore,
    List<WritingFeedback> Feedbacks
);

// ✅ NEW: Unified Exam DTOs
public record FinishExamResponse(Guid SessionId, double OverallScore, string FeedbackSummary);

// ✅ NEW: Speaking DTOs
public record SubmitSpeakingItem(string PartId, string Transcript);

public record SubmitSpeakingRequest(Guid SessionId, List<SubmitSpeakingItem> Answers);

public record SpeakingFeedback(
    string PartId,
    int Score,
    string PronunciationFeedback,
    string FluencyFeedback,
    string ContentFeedback,
    string OverallComment
);

public record SubmitSpeakingResponse(
    Guid SessionId,
    int TotalScore,
    List<SpeakingFeedback> Feedbacks
);