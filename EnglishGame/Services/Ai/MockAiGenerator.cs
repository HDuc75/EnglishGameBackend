using System.Text.Json;
using EnglishGame.Models;
using EnglishGame.Dtos;
using EnglishGame.Services.Vocabulary;

namespace EnglishGame.Services.Ai;

public class MockAiGenerator : IAiGenerator
{
    public Task<(string prompt, string rawResponse, string contentJson)> GenerateQuizAsync(
        string topic, string level, int count, CancellationToken ct)
    {
        var prompt = $"[MOCK] Generate quiz: topic={topic}, level={level}, count={count}";
        var json = BuildMockJson(topic, level, "vocabulary", count, 1);
        return Task.FromResult((prompt, "[MOCK_RAW]", json));
    }

    public Task<(string prompt, string rawResponse, string contentJson)> GenerateSkillQuizAsync(
        SkillType skill, WordData[] words, string topic, string level, int count, CancellationToken ct)
    {
        var skillName = skill.ToString().ToLower();
        var prompt = $"[MOCK] Generate {skillName} quiz: topic={topic}, level={level}, count={count}";
        var json = BuildMockJson(topic, level, skillName, count, 1);
        return Task.FromResult((prompt, "[MOCK_RAW]", json));
    }

    public Task<(string prompt, string rawResponse, string contentJson)> GenerateAllSkillsAtOnceAsync(
        WordData[] words, string topic, string level,
        int vocabCount, int readingCount, int listenCount, int grammarCount, int speakingCount,
        CancellationToken ct)
    {
        var prompt = $"[MOCK] GenerateAllSkills: topic={topic}, level={level}";
        var allSkills = new (string skill, int count)[]
        {
            ("vocabulary", vocabCount), ("reading", readingCount),
            ("listening", listenCount), ("grammar", grammarCount), ("speaking", speakingCount)
        };

        var no = 1;
        var questions = allSkills.SelectMany(s =>
            Enumerable.Range(0, s.count).Select(_ =>
            {
                var q = new
                {
                    no = no++,
                    skill = s.skill,
                    format = "mcq",
                    question = $"[{s.skill.ToUpper()}] ({level}) {topic} — Question {no}?",
                    options = new[] { "A. Option A", "B. Option B", "C. Option C", "D. Option D" },
                    answer = "A. Option A",
                    explanation = $"Mock {s.skill} explanation."
                };
                return q;
            })
        );

        var json = JsonSerializer.Serialize(new { type = "quiz", level, topic, questions });
        return Task.FromResult((prompt, "[MOCK_RAW]", json));
    }

    public Task<string> ChatAsync(List<(string role, string content)> messages, CancellationToken ct)
    {
        var last = messages.LastOrDefault(m => m.role == "user").content ?? "";
        return Task.FromResult($"(Mock) You said: {last}");
    }

    public Task<(string prompt, string rawResponse, string contentJson)> GenerateWritingPromptAsync(
        string topic, string level, CancellationToken ct)
    {
        var prompt = $"[MOCK] Generate writing prompt: topic={topic}, level={level}";
        var json = JsonSerializer.Serialize(new
        {
            type = "writing_prompt",
            level,
            topic,
            task1 = new { title = "Task 1", prompt = "Write an email...", minWords = 120 },
            task2 = new { title = "Task 2", prompt = "Write an essay...", minWords = 250 }
        });
        return Task.FromResult((prompt, "[MOCK_RAW]", json));
    }

    public Task<(string prompt, string rawResponse, string contentJson)> GenerateSpeakingPromptAsync(
        string topic, string level, CancellationToken ct)
    {
        var prompt = $"[MOCK] Generate speaking prompt: topic={topic}, level={level}";
        var json = JsonSerializer.Serialize(new
        {
            type = "speaking_prompt",
            level,
            topic,
            part1 = new[] {
                new { theme = "Daily Routine", questions = new[] { "What time do you wake up?", "Do you have a busy morning?" } }
            },
            part2 = new {
                situation = "You are going on a trip. Choose transportation.",
                options = new[] { "Bus", "Train", "Airplane" },
                question = "Which option is best for you and why?"
            },
            part3 = new {
                topic = "Benefits of Travel",
                bulletPoints = new[] { "Learn cultures", "Relax", "Make friends" },
                followUpQuestions = new[] { "Is traveling expensive in your country?" }
            }
        });
        return Task.FromResult((prompt, "[MOCK_RAW]", json));
    }

    public Task<WritingFeedback> EvaluateWritingAsync(
        string topic, string level, string taskId, string taskPrompt, string userContent, int minWords, CancellationToken ct)
    {
        return Task.FromResult(new WritingFeedback(
            taskId,
            8,
            "Mock grammar feedback",
            "Mock vocabulary feedback",
            "Mock coherence feedback",
            "Mock task fulfillment feedback",
            "Mock overall comment"
        ));
    }

    public Task<SpeakingFeedback> EvaluateSpeakingAsync(
        string topic, string level, string partId, string prompt, string userTranscript, CancellationToken ct)
    {
        return Task.FromResult(new SpeakingFeedback(
            partId,
            7,
            "Mock pronunciation feedback: Generally clear with minor accent interference.",
            "Mock fluency feedback: Speech was mostly fluent with some hesitations.",
            "Mock content feedback: Ideas were relevant and adequately developed.",
            "Good effort! Keep practicing for more natural delivery."
        ));
    }

    private static string BuildMockJson(string topic, string level, string skill, int count, int startNo)
    {
        var questions = Enumerable.Range(startNo, count).Select(i => new
        {
            no = i,
            skill,
            format = "mcq",
            question = $"[{skill.ToUpper()}] ({level}) {topic} — Question {i}?",
            options = new[] { "A. Option A", "B. Option B", "C. Option C", "D. Option D" },
            answer = "A. Option A",
            explanation = $"Mock explanation for {skill} question {i}."
        });

        return JsonSerializer.Serialize(new { type = "quiz", level, topic, questions });
    }
}
