using EnglishGame.Data;
using EnglishGame.Dtos;
using EnglishGame.Models;
using EnglishGame.Services.Ai;
using EnglishGame.Services.Vocabulary;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace EnglishGame.Services;

public class GameService
{
    private readonly AppDbContext _db;
    private readonly IAiGenerator _ai;
    private readonly IConfiguration _cfg;
    private readonly VocabularyService _vocab;
    private readonly ILogger<GameService> _logger;

    public GameService(AppDbContext db, IAiGenerator ai, IConfiguration cfg, VocabularyService vocab, ILogger<GameService> logger)
    {
        _db = db;
        _ai = ai;
        _cfg = cfg;
        _vocab = vocab;
        _logger = logger;
    }

    public async Task<StartGameResponse> StartAsync(ClaimsPrincipal user, StartGameRequest req, CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!);

        var topic = await _db.Topics
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == req.TopicId && t.IsActive, ct)
            ?? throw new InvalidOperationException("Topic not found.");

        // ✅ Determine Exam Types
        var isFullExam = req.Mode == GameMode.FullExam;
        var isWriting = req.Mode == GameMode.Writing;
        var questionCount = isFullExam ? 75 : (isWriting ? 0 : 75);
        var targetType = isFullExam ? AiContentType.FullExam : (isWriting ? AiContentType.WritingPrompt : AiContentType.Quiz);

        var autoApprove = bool.TryParse(_cfg["Ai:AutoApproveGeneratedContent"], out var b) ? b : true;

        async Task<AiContent> GenerateAndSaveNewContentAsync()
        {
            string json;
            string rawResponse;
            string rawPrompt;

            if (isFullExam)
            {
                var tq = GenerateSkillMixedQuizAsync(topic, req.Level.ToString(), 75, userId, ct);
                var tw = _ai.GenerateWritingPromptAsync(topic.Name, req.Level.ToString(), ct);
                var ts = _ai.GenerateSpeakingPromptAsync(topic.Name, req.Level.ToString(), ct);
                
                await Task.WhenAll(tq, tw, ts);
                
                var fullObj = new {
                    type = "full_exam",
                    quiz = JsonDocument.Parse(tq.Result).RootElement.Clone(),
                    writing = JsonDocument.Parse(tw.Result.contentJson).RootElement.Clone(),
                    speaking = JsonDocument.Parse(ts.Result.contentJson).RootElement.Clone()
                };
                json = JsonSerializer.Serialize(fullObj);
                rawResponse = "[FULL-EXAM-MERGED]";
                rawPrompt = $"[FULL-EXAM] topic={topic.Name} level={req.Level}";
            }
            else if (isWriting)
            {
                var res = await _ai.GenerateWritingPromptAsync(topic.Name, req.Level.ToString(), ct);
                json = res.contentJson;
                rawResponse = "[WRITING-PROMPT]";
                rawPrompt = $"[WRITING] topic={topic.Name} level={req.Level}";
            }
            else
            {
                json = await GenerateSkillMixedQuizAsync(topic, req.Level.ToString(), questionCount, userId, ct);
                rawResponse = "[5-SKILL-MERGED]";
                rawPrompt = $"[5-SKILL] topic={topic.Name} level={req.Level} count={questionCount}";
                if (!TryGetQuestionCount(json, out var cnt) || cnt <= 0)
                    throw new InvalidOperationException("AI returned invalid quiz JSON.");
            }

            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("AI returned empty content.");

            var newC = new AiContent
            {
                Type = targetType,
                Level = req.Level,
                TopicId = topic.Id,
                Status = autoApprove ? ReviewStatus.Approved : ReviewStatus.Pending,
                RawPrompt = rawPrompt,
                RawResponse = rawResponse,
                ContentJson = json,
                CreatedByUserId = userId,
                ReviewedByUserId = autoApprove ? userId : null,
                ReviewedAtUtc = autoApprove ? DateTimeOffset.UtcNow : null
            };

            _db.AiContents.Add(newC);
            await _db.SaveChangesAsync(ct);

            if (newC.Status != ReviewStatus.Approved)
                throw new InvalidOperationException("No approved content yet. Ask admin to approve generated content.");

            return newC;
        }

        AiContent? content = null;
        if (req.ForceAi)
        {
            content = await GenerateAndSaveNewContentAsync();
        }
        else
        {
            var candidates = await _db.AiContents
                .AsNoTracking()
                .Where(x => x.Type == targetType && x.Level == req.Level && x.TopicId == req.TopicId && x.Status == ReviewStatus.Approved)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new { x.Id, x.ContentJson })
                .Take(20)
                .ToListAsync(ct);

            if (isWriting || isFullExam)
            {
                var validCandidate = candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.ContentJson));
                if (validCandidate != null) content = await _db.AiContents.FirstAsync(x => x.Id == validCandidate.Id, ct);
            }
            else
            {
                var skillCandidates = candidates.Where(c => IsSkillBasedContent(c.ContentJson)).ToList();
                foreach (var c in skillCandidates)
                {
                    if (TryGetQuestionCount(c.ContentJson, out var cnt) && cnt == questionCount)
                    {
                        content = await _db.AiContents.FirstAsync(x => x.Id == c.Id, ct);
                        break;
                    }
                }
                if (content == null)
                {
                    var bigger = skillCandidates.Select(c => { var ok = TryGetQuestionCount(c.ContentJson, out var cnt); return new { c.Id, c.ContentJson, Ok = ok, Count = ok ? cnt : -1 }; }).Where(x => x.Ok && x.Count > questionCount).OrderByDescending(x => x.Count).FirstOrDefault();
                    if (bigger != null)
                    {
                        var slicedJson = SliceQuizJson(bigger.ContentJson, questionCount);
                        content = new AiContent { Type = AiContentType.Quiz, Level = req.Level, TopicId = topic.Id, Status = autoApprove ? ReviewStatus.Approved : ReviewStatus.Pending, RawPrompt = $"[SLICED from {bigger.Id}]", RawResponse = "[SLICED]", ContentJson = slicedJson, CreatedByUserId = userId, ReviewedByUserId = autoApprove ? userId : null, ReviewedAtUtc = autoApprove ? DateTimeOffset.UtcNow : null };
                        _db.AiContents.Add(content);
                        await _db.SaveChangesAsync(ct);
                        if (content.Status != ReviewStatus.Approved) throw new InvalidOperationException("No approved content yet.");
                    }
                }
            }

            if (content == null) content = await GenerateAndSaveNewContentAsync();
        }

        var session = new GameSession
        {
            UserId = userId,
            Mode = req.Mode,
            Level = req.Level,
            TopicId = topic.Id,
            AiContentId = content!.Id
        };

        _db.GameSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        var clientContentObj = StripAnswersForClient(content.ContentJson);

        return new StartGameResponse(
            session.Id,
            topic.Id,
            topic.Name,
            req.Level,
            req.Mode,
            clientContentObj
        );
    }

    public async Task<SubmitGameResponse> SubmitAsync(ClaimsPrincipal user, SubmitGameRequest req, CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!);

        var session = await _db.GameSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.SessionId && s.UserId == userId, ct)
            ?? throw new InvalidOperationException("Session not found.");

        if (session.FinishedAtUtc != null)
            throw new InvalidOperationException("Session already finished.");

        if (session.AiContentId == Guid.Empty)
            throw new InvalidOperationException("Session content is missing. Please restart the game.");

        var content = await _db.AiContents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == session.AiContentId, ct);

        if (content == null || string.IsNullOrWhiteSpace(content.ContentJson))
            throw new InvalidOperationException("Quiz content not found. Please restart the game.");

        using var quiz = JsonDocument.Parse(content.ContentJson);

        // FullExam stores questions nested under "quiz.questions"; standalone Quiz stores at root
        JsonElement questionsEl;
        if (quiz.RootElement.TryGetProperty("quiz", out var quizEl) && quizEl.TryGetProperty("questions", out questionsEl))
        {
            // FullExam path
        }
        else if (!quiz.RootElement.TryGetProperty("questions", out questionsEl) || questionsEl.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Quiz content missing 'questions' array.");
        }

        var questions = questionsEl.EnumerateArray()
            .Select(q => new
            {
                No = q.GetProperty("no").GetInt32(),
                Answer = q.GetProperty("answer").GetString() ?? "",
                Explanation = q.TryGetProperty("explanation", out var exp) ? (exp.GetString() ?? "—") : "—"
            })
            .ToDictionary(x => x.No, x => x);

        if (questions.Count == 0)
            throw new InvalidOperationException("Quiz has no questions.");

        var totalQuestions = questions.Count;
        var basePoints = 100 / totalQuestions;
        var remainder = 100 % totalQuestions;
        var bonusSet = questions.Keys.OrderBy(x => x).Take(remainder).ToHashSet();

        var trackedSession = await _db.GameSessions
            .FirstAsync(s => s.Id == req.SessionId && s.UserId == userId, ct);

        var results = new List<PerQuestionResult>();
        var total = 0;

        foreach (var a in req.Answers)
        {
            if (!questions.TryGetValue(a.No, out var q))
                continue;

            var userAns = a.Answer?.Trim() ?? "";
            var correctAns = q.Answer.Trim();

            var isCorrect = string.Equals(userAns, correctAns, StringComparison.OrdinalIgnoreCase);

            var score = 0;
            if (isCorrect)
                score = basePoints + (bonusSet.Contains(a.No) ? 1 : 0);

            total += score;

            results.Add(new PerQuestionResult(a.No, isCorrect, score, q.Explanation));

            _db.Attempts.Add(new Attempt
            {
                GameSessionId = trackedSession.Id,
                QuestionNo = a.No,
                UserAnswer = userAns,
                IsCorrect = isCorrect,
                Score = score,
                Explanation = q.Explanation
            });
        }

        trackedSession.TotalScore = total;
        if (trackedSession.Mode != GameMode.FullExam)
            trackedSession.FinishedAtUtc = DateTimeOffset.UtcNow;

        var u = await _db.Users.FirstAsync(x => x.Id == userId, ct);
        u.Xp += Math.Max(5, total / 10);

        await _db.SaveChangesAsync(ct);

        return new SubmitGameResponse(trackedSession.Id, total, results.OrderBy(r => r.No).ToList());
    }

    public async Task<SubmitWritingResponse> SubmitWritingAsync(ClaimsPrincipal user, SubmitWritingRequest req, CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!);

        var session = await _db.GameSessions
            .FirstOrDefaultAsync(s => s.Id == req.SessionId && s.UserId == userId, ct)
            ?? throw new InvalidOperationException("Session not found.");

        if (session.FinishedAtUtc != null)
            throw new InvalidOperationException("Session already finished.");

        var content = await _db.AiContents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == session.AiContentId, ct);

        if (content == null || string.IsNullOrWhiteSpace(content.ContentJson))
            throw new InvalidOperationException("Writing prompt not found.");

        var topicName = await _db.Topics.Where(t => t.Id == session.TopicId).Select(t => t.Name).FirstOrDefaultAsync(ct) ?? "General";

        using var quiz = JsonDocument.Parse(content.ContentJson);
        var root = quiz.RootElement;

        // FullExam nests writing content under "writing" key; standalone Writing mode has it at root
        if (root.TryGetProperty("writing", out var writingEl))
            root = writingEl;

        var feedbacks = new List<WritingFeedback>();
        var totalScore = 0;

        foreach (var answer in req.Answers)
        {
            if (!root.TryGetProperty(answer.TaskId, out var taskEl))
                continue;

            var title = taskEl.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : "";
            var promptStr = taskEl.TryGetProperty("prompt", out var promptEl) ? promptEl.GetString() ?? "" : "";
            var minWords = taskEl.TryGetProperty("minWords", out var minWordsEl) && minWordsEl.ValueKind == JsonValueKind.Number ? minWordsEl.GetInt32() : 120;

            var feedback = await _ai.EvaluateWritingAsync(topicName, session.Level.ToString(), answer.TaskId, promptStr, answer.Content, minWords, ct);
            feedbacks.Add(feedback);
            totalScore += feedback.Score;

            var explanationJson = JsonSerializer.Serialize(feedback);

            _db.Attempts.Add(new Attempt
            {
                GameSessionId = session.Id,
                QuestionNo = answer.TaskId == "task1" ? 1001 : 1002,
                UserAnswer = answer.Content,
                IsCorrect = feedback.Score >= 5, // Arbitrary pass mark
                Score = feedback.Score,
                Explanation = explanationJson
            });
        }

        // VSTEP is technically out of 10, totalScore will be sum of tasks (e.g. out of 20). Convert back to percentage standard
        session.TotalScore = feedbacks.Count > 0 ? (totalScore * 100) / (feedbacks.Count * 10) : 0;
        
        if (session.Mode != GameMode.FullExam)
            session.FinishedAtUtc = DateTimeOffset.UtcNow;

        var u = await _db.Users.FirstAsync(x => x.Id == userId, ct);
        u.Xp += Math.Max(5, session.TotalScore / 5);

        await _db.SaveChangesAsync(ct);

        return new SubmitWritingResponse(session.Id, session.TotalScore, feedbacks);
    }

    public async Task<SubmitSpeakingResponse> SubmitSpeakingAsync(ClaimsPrincipal user, SubmitSpeakingRequest req, CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!);

        var session = await _db.GameSessions
            .FirstOrDefaultAsync(s => s.Id == req.SessionId && s.UserId == userId, ct)
            ?? throw new InvalidOperationException("Session not found.");

        if (session.FinishedAtUtc != null)
            throw new InvalidOperationException("Session already finished.");

        var content = await _db.AiContents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == session.AiContentId, ct);

        if (content == null || string.IsNullOrWhiteSpace(content.ContentJson))
            throw new InvalidOperationException("Speaking prompt not found.");

        var topicName = await _db.Topics.Where(t => t.Id == session.TopicId).Select(t => t.Name).FirstOrDefaultAsync(ct) ?? "General";

        using var doc = JsonDocument.Parse(content.ContentJson);
        var root = doc.RootElement;

        // FullExam nests speaking under "speaking" key
        if (root.TryGetProperty("speaking", out var speakingEl))
            root = speakingEl;

        var feedbacks = new List<SpeakingFeedback>();
        var totalScore = 0;

        // Map part IDs to question numbers (offset 2001+)
        var partNoMap = new Dictionary<string, int> { ["part1"] = 2001, ["part2"] = 2002, ["part3"] = 2003 };

        foreach (var answer in req.Answers)
        {
            // Build friendly prompt string from the speaking structure
            var promptStr = answer.PartId switch
            {
                "part1" when root.TryGetProperty("part1", out var p1El) =>
                    p1El.ValueKind == JsonValueKind.Array
                        ? string.Join(" | ", p1El.EnumerateArray().SelectMany(t => t.TryGetProperty("questions", out var qs) ? qs.EnumerateArray().Select(q => q.GetString() ?? "") : Enumerable.Empty<string>()))
                        : "Speak on provided topic",
                "part2" when root.TryGetProperty("part2", out var p2El) =>
                    (p2El.TryGetProperty("situation", out var sit) ? sit.GetString() ?? "" : "") + " | " +
                    (p2El.TryGetProperty("question", out var q2) ? q2.GetString() ?? "" : ""),
                "part3" when root.TryGetProperty("part3", out var p3El) =>
                    (p3El.TryGetProperty("topic", out var t3) ? t3.GetString() ?? "" : "") + " | " +
                    (p3El.TryGetProperty("followUpQuestions", out var fq) && fq.ValueKind == JsonValueKind.Array
                        ? string.Join(" ", fq.EnumerateArray().Select(q => q.GetString() ?? ""))
                        : ""),
                _ => "General speaking prompt"
            };

            var feedback = await _ai.EvaluateSpeakingAsync(topicName, session.Level.ToString(), answer.PartId, promptStr, answer.Transcript, ct);
            feedbacks.Add(feedback);
            totalScore += feedback.Score;

            var explanationJson = System.Text.Json.JsonSerializer.Serialize(feedback);
            var questionNo = partNoMap.GetValueOrDefault(answer.PartId, 2001 + feedbacks.Count - 1);

            _db.Attempts.Add(new Attempt
            {
                GameSessionId = session.Id,
                QuestionNo = questionNo,
                UserAnswer = answer.Transcript,
                IsCorrect = feedback.Score >= 5,
                Score = feedback.Score,
                Explanation = explanationJson
            });
        }

        // Don't finish session yet if FullExam
        if (session.Mode != GameMode.FullExam)
            session.FinishedAtUtc = DateTimeOffset.UtcNow;

        var u = await _db.Users.FirstAsync(x => x.Id == userId, ct);
        u.Xp += Math.Max(5, totalScore);

        await _db.SaveChangesAsync(ct);

        return new SubmitSpeakingResponse(session.Id, totalScore, feedbacks);
    }

    public async Task<FinishExamResponse> FinishExamAsync(ClaimsPrincipal user, Guid sessionId, CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!);

        var session = await _db.GameSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct)
            ?? throw new InvalidOperationException("Session not found.");

        if (session.FinishedAtUtc != null)
            throw new InvalidOperationException("Exam already finished.");

        var attempts = await _db.Attempts.Where(a => a.GameSessionId == sessionId).ToListAsync(ct);
        
        double quizDbScore = attempts.Where(a => a.QuestionNo <= 100).Sum(a => a.Score);
        double writingDbScore = attempts.Where(a => a.QuestionNo == 1001 || a.QuestionNo == 1002).Sum(a => a.Score);
        
        // Rough VSTEP translation: Listening/Reading 0-10, Writing 0-10.
        // Quiz generates 100 basePoints across 75 questions -> quizDbScore is 0-100. Let's divide by 10.
        double quizBand = Math.Clamp(quizDbScore / 10.0, 0, 10);
        // WritingDbScore is sum of 2 tasks (0-10 max each, so 0-20). Let's divide by 2.
        double writingBand = Math.Clamp(writingDbScore / 2.0, 0, 10);
        
        // Overall: average of 3 skills for now (Reading, Listening, Writing)
        // Wait, Quiz contains both Reading and Listening. For simplicity, just count Quiz as 2 skills.
        double overallScore = Math.Round((quizBand * 2 + writingBand) / 3.0, 1);

        session.TotalScore = (int)(overallScore * 10); // store as percentage 0-100
        session.FinishedAtUtc = DateTimeOffset.UtcNow;

        var u = await _db.Users.FirstAsync(x => x.Id == userId, ct);
        u.Xp += 50; // Bonus XP for full exam!

        await _db.SaveChangesAsync(ct);

        return new FinishExamResponse(session.Id, overallScore, "VSTEP Exam Completed Successfully!");
    }

    public async Task<object> GetHistoryAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!);

        // ✅ không Include navigation, tránh Topic null
        var list = await _db.GameSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartedAtUtc)
            .Select(s => new
            {
                s.Id,
                s.Mode,
                s.Level,
                topic = _db.Topics.Where(t => t.Id == s.TopicId).Select(t => t.Name).FirstOrDefault() ?? "—",
                s.TotalScore,
                s.StartedAtUtc,
                s.FinishedAtUtc
            })
            .ToListAsync(ct);

        return list;
    }

    public async Task<object> GetSessionAsync(ClaimsPrincipal user, Guid sessionId, CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!);

        var session = await _db.GameSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct)
            ?? throw new InvalidOperationException("Session not found.");

        var topicName = await _db.Topics
            .AsNoTracking()
            .Where(t => t.Id == session.TopicId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct) ?? "—";

        var content = await _db.AiContents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == session.AiContentId, ct);

        if (content == null || string.IsNullOrWhiteSpace(content.ContentJson))
            throw new InvalidOperationException("Quiz content not found.");

        var attempts = await _db.Attempts
            .AsNoTracking()
            .Where(a => a.GameSessionId == session.Id)
            .Select(a => new { no = a.QuestionNo, answer = a.UserAnswer })
            .ToListAsync(ct);

        return new
        {
            sessionId = session.Id,
            topicId = session.TopicId,
            topic = topicName,
            level = session.Level,
            mode = session.Mode,
            startedAtUtc = session.StartedAtUtc,
            finishedAtUtc = session.FinishedAtUtc,
            content = StripAnswersForClient(content.ContentJson),
            answers = attempts
        };
    }

    public async Task<object> GetReviewAsync(ClaimsPrincipal user, Guid sessionId, CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!);

        var session = await _db.GameSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct)
            ?? throw new InvalidOperationException("Session not found.");

        var topicName = await _db.Topics
            .AsNoTracking()
            .Where(t => t.Id == session.TopicId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct) ?? "—";

        if (session.AiContentId == Guid.Empty)
            throw new InvalidOperationException("Session content is missing. Please restart the game.");

        var content = await _db.AiContents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == session.AiContentId, ct);

        if (content == null || string.IsNullOrWhiteSpace(content.ContentJson))
            throw new InvalidOperationException("Quiz content not found.");

        var clientContentObj = StripAnswersForClient(content.ContentJson);

        if (session.Mode == GameMode.Writing)
        {
            var attemptsW = await _db.Attempts
                .AsNoTracking()
                .Where(a => a.GameSessionId == sessionId)
                .OrderBy(a => a.QuestionNo)
                .Select(a => new
                {
                    taskId = a.QuestionNo == 1 ? "task1" : "task2",
                    userAnswer = a.UserAnswer,
                    score = a.Score,
                    explanation = a.Explanation
                })
                .ToListAsync(ct);

            return new
            {
                sessionId = session.Id,
                topic = topicName,
                level = session.Level,
                mode = session.Mode,
                totalScore = session.TotalScore,
                startedAtUtc = session.StartedAtUtc,
                finishedAtUtc = session.FinishedAtUtc,
                content = clientContentObj,
                feedbacks = attemptsW
            };
        }

        using var quiz = JsonDocument.Parse(content.ContentJson);

        if (!quiz.RootElement.TryGetProperty("questions", out var questionsEl) || questionsEl.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Quiz content missing 'questions' array.");

        var correctAnswers = questionsEl.EnumerateArray()
            .Where(q => q.TryGetProperty("no", out _) && q.TryGetProperty("answer", out _))
            .ToDictionary(
                q => q.GetProperty("no").GetInt32(),
                q => (q.GetProperty("answer").GetString() ?? "").Trim()
            );

        var attempts = await _db.Attempts
            .AsNoTracking()
            .Where(a => a.GameSessionId == sessionId)
            .OrderBy(a => a.QuestionNo)
            .Select(a => new
            {
                no = a.QuestionNo,
                userAnswer = a.UserAnswer,
                isCorrect = a.IsCorrect,
                score = a.Score,
                explanation = a.Explanation
            })
            .ToListAsync(ct);

        return new
        {
            sessionId = session.Id,
            topic = topicName,
            level = session.Level,
            mode = session.Mode,
            totalScore = session.TotalScore,
            startedAtUtc = session.StartedAtUtc,
            finishedAtUtc = session.FinishedAtUtc,
            content = clientContentObj,
            attempts,
            correctAnswers
        };
    }

    // ── 5-Skill Quiz Generator ────────────────────────────────────────────────

    /// <summary>
    /// Generates a mixed quiz across 5 skills according to configured distribution.
    /// Default: Vocabulary 15%, Reading 30%, Listening 20%, Grammar 20%, Speaking 15%.
    /// </summary>
    private async Task<string> GenerateSkillMixedQuizAsync(
        Topic topic, string level, int totalCount, Guid userId, CancellationToken ct)
    {
        // ── VSTEP STRICT DISTRIBUTION (75 Questions) ──────────────────────────
        var counts = new Dictionary<SkillType, int>
        {
            [SkillType.Vocabulary] = 0,
            [SkillType.Reading]    = 40,
            [SkillType.Listening]  = 35,
            [SkillType.Grammar]    = 0,
            [SkillType.Speaking]   = 0,
        };

        totalCount = 75;

        // Bỏ qua load từ vựng nếu count = 0
        var vocabWords = Array.Empty<WordData>();

        // ── PRIMARY: generate all 5 skills in ONE AI call (fast ~1-2 min) ────
        var allQuestionRaws = new List<string>();
        bool singleCallSucceeded = false;

        try
        {
            _logger.LogInformation("GenerateSkillMixedQuizAsync: trying single-call approach for topic={Topic}", topic.Name);
            var aiResult = await _ai.GenerateAllSkillsAtOnceAsync(
                vocabWords, topic.Name, level,
                counts[SkillType.Vocabulary], counts[SkillType.Reading],
                counts[SkillType.Listening], counts[SkillType.Grammar], counts[SkillType.Speaking],
                ct);
            var singleJson = aiResult.contentJson;

            if (!string.IsNullOrWhiteSpace(singleJson))
            {
                using var doc = JsonDocument.Parse(singleJson);
                if (doc.RootElement.TryGetProperty("questions", out var qArr) && qArr.ValueKind == JsonValueKind.Array && qArr.GetArrayLength() > 0)
                {
                    foreach (var q in qArr.EnumerateArray())
                        allQuestionRaws.Add(q.GetRawText());
                    singleCallSucceeded = true;
                    _logger.LogInformation("GenerateSkillMixedQuizAsync: single-call got {Count} questions", allQuestionRaws.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GenerateSkillMixedQuizAsync: single-call failed, will fallback to per-skill");
        }

        // ── FALLBACK: per-skill sequential (slower but more reliable) ──────────
        if (!singleCallSucceeded)
        {
            _logger.LogInformation("GenerateSkillMixedQuizAsync: falling back to per-skill sequential generation");
            foreach (var (skill, count) in counts)
            {
                if (ct.IsCancellationRequested) break;

                string? skillJson = null;
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        var aiResult = await _ai.GenerateSkillQuizAsync(skill, vocabWords, topic.Name, level, count, ct);
                        skillJson = aiResult.contentJson;
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Skill {Skill} attempt {Attempt} failed", skill, attempt + 1);
                    }
                }

                if (string.IsNullOrWhiteSpace(skillJson)) continue;

                try
                {
                    using var doc = JsonDocument.Parse(skillJson);
                    if (doc.RootElement.TryGetProperty("questions", out var qArr) && qArr.ValueKind == JsonValueKind.Array)
                        foreach (var q in qArr.EnumerateArray())
                            allQuestionRaws.Add(q.GetRawText());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse {Skill} response", skill);
                }
            }
        }

        if (allQuestionRaws.Count == 0)
            throw new InvalidOperationException("Quiz generation failed. Please check AI configuration and try again.");

        // ── Merge + re-number ──────────────────────────────────────────────────
        using var outStream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(outStream))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "quiz");
            writer.WriteString("level", level);
            writer.WriteString("topic", topic.Name);
            writer.WritePropertyName("questions");
            writer.WriteStartArray();

            var no = 1;
            foreach (var raw in allQuestionRaws)
            {
                try
                {
                    using var qDoc = JsonDocument.Parse(raw);
                    writer.WriteStartObject();
                    writer.WriteNumber("no", no++);
                    foreach (var prop in qDoc.RootElement.EnumerateObject())
                    {
                        if (prop.Name == "no") continue;
                        writer.WritePropertyName(prop.Name);
                        prop.Value.WriteTo(writer);
                    }
                    writer.WriteEndObject();
                }
                catch { /* skip malformed question */ }
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(outStream.ToArray());
    }


    // ── Helpers ─────────────────────────────────────────────────────────────

    private static object StripAnswersForClient(string contentJson)
    {
        using var doc = JsonDocument.Parse(contentJson);
        var root = doc.RootElement;
        var type = root.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;

        if (type == "writing_prompt")
        {
            return root.Clone();
        }

        if (type == "full_exam")
        {
            var quizData = root.TryGetProperty("quiz", out var quizObj) ? StripQuestionsArray(quizObj) : Array.Empty<object>();
            return new
            {
                type,
                level = root.TryGetProperty("level", out var l) ? l.GetString() : null,
                topic = root.TryGetProperty("topic", out var t) ? t.GetString() : null,
                quiz = new { questions = quizData },
                writing = root.TryGetProperty("writing", out var w) ? (object)w.Clone() : null,
                speaking = root.TryGetProperty("speaking", out var s) ? (object)s.Clone() : null
            };
        }

        return new
        {
            type,
            level = root.TryGetProperty("level", out var levelEl) ? levelEl.GetString() : null,
            topic = root.TryGetProperty("topic", out var topicEl) ? topicEl.GetString() : null,
            questions = StripQuestionsArray(root)
        };
    }

    private static object[] StripQuestionsArray(JsonElement root)
    {
        if (!root.TryGetProperty("questions", out var qProp) || qProp.ValueKind != JsonValueKind.Array) return Array.Empty<object>();
        
        return qProp.EnumerateArray().Select(q => new
        {
            no = q.GetProperty("no").GetInt32(),
            skill = q.TryGetProperty("skill", out var sk) ? sk.GetString() : null,
            format = q.TryGetProperty("format", out var f) ? f.GetString() : null,
            passage = q.TryGetProperty("passage", out var p) ? p.GetString() : null,
            section = q.TryGetProperty("section", out var s) ? s.GetString() : null,
            passageId = q.TryGetProperty("passageId", out var pid) ? pid.GetString() : null,
            question = q.GetProperty("question").GetString(),
            options = q.TryGetProperty("options", out var opt)
                ? opt.EnumerateArray().Select(x => x.GetString() ?? "").ToArray()
                : Array.Empty<string>()
        }).ToArray();
    }

    private static bool TryGetQuestionCount(string contentJson, out int count)
    {
        count = 0;
        try
        {
            using var doc = JsonDocument.Parse(contentJson);
            if (!doc.RootElement.TryGetProperty("questions", out var qEl) || qEl.ValueKind != JsonValueKind.Array)
                return false;

            count = qEl.GetArrayLength();
            return count >= 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true if the content JSON was generated by the 5-skill pipeline
    /// (i.e., at least the first question has a non-empty "skill" property).
    /// Old mock/test content without "skill" field returns false.
    /// </summary>
    private static bool IsSkillBasedContent(string contentJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(contentJson);
            if (!doc.RootElement.TryGetProperty("questions", out var qEl) || qEl.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var q in qEl.EnumerateArray())
            {
                // Check first question only — all 5-skill content has skill on every question
                return q.TryGetProperty("skill", out var sk) &&
                       sk.ValueKind == JsonValueKind.String &&
                       !string.IsNullOrWhiteSpace(sk.GetString());
            }
            return false;
        }
        catch { return false; }
    }

    private static string SliceQuizJson(string contentJson, int takeCount)
    {
        using var doc = JsonDocument.Parse(contentJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("questions", out var qEl) || qEl.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Quiz content missing 'questions' array.");

        var list = qEl.EnumerateArray().Take(takeCount).ToArray();

        using var outStream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(outStream))
        {
            writer.WriteStartObject();

            if (root.TryGetProperty("type", out var typeEl))
            {
                writer.WritePropertyName("type");
                typeEl.WriteTo(writer);
            }
            if (root.TryGetProperty("level", out var levelEl))
            {
                writer.WritePropertyName("level");
                levelEl.WriteTo(writer);
            }
            if (root.TryGetProperty("topic", out var topicEl))
            {
                writer.WritePropertyName("topic");
                topicEl.WriteTo(writer);
            }

            writer.WritePropertyName("questions");
            writer.WriteStartArray();
            foreach (var item in list) item.WriteTo(writer);
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(outStream.ToArray());
    }
}