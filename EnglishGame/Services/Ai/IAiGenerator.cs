using EnglishGame.Models;
using EnglishGame.Services.Vocabulary;

namespace EnglishGame.Services.Ai;

public interface IAiGenerator
{
    /// <summary>Legacy: generate quiz purely by topic name (no word data).</summary>
    Task<(string prompt, string rawResponse, string contentJson)> GenerateQuizAsync(
        string topic, string level, int count, CancellationToken ct);

    /// <summary>
    /// Generate questions for a specific skill (used as fallback per-skill).
    /// </summary>
    Task<(string prompt, string rawResponse, string contentJson)> GenerateSkillQuizAsync(
        SkillType skill, WordData[] words, string topic, string level, int count, CancellationToken ct);

    /// <summary>
    /// Generate ALL 5 skills in a SINGLE AI call — much faster than 5 separate calls.
    /// Returns merged contentJson with all questions numbered sequentially.
    /// </summary>
    Task<(string prompt, string rawResponse, string contentJson)> GenerateAllSkillsAtOnceAsync(
        WordData[] words, string topic, string level,
        int vocabCount, int readingCount, int listenCount, int grammarCount, int speakingCount,
        CancellationToken ct);

    /// <summary>
    /// Generates VSTEP Writing Prompts (Task 1 Email, Task 2 Essay).
    /// </summary>
    Task<(string prompt, string rawResponse, string contentJson)> GenerateWritingPromptAsync(
        string topic, string level, CancellationToken ct);

    /// <summary>
    /// Evaluates a submitted writing task (Email or Essay) using VSTEP criteria.
    /// </summary>
    Task<EnglishGame.Dtos.WritingFeedback> EvaluateWritingAsync(
        string topic, string level, string taskId, string taskPrompt, string userContent, int minWords, CancellationToken ct);

    /// <summary>
    /// Generates VSTEP Speaking Prompts (Parts 1, 2, and 3).
    /// </summary>
    Task<(string prompt, string rawResponse, string contentJson)> GenerateSpeakingPromptAsync(
        string topic, string level, CancellationToken ct);

    /// <summary>Chat-style conversation (used by AiChatController).</summary>
    Task<string> ChatAsync(List<(string role, string content)> messages, CancellationToken ct);

    /// <summary>
    /// Evaluates a submitted speaking response using VSTEP criteria.
    /// </summary>
    Task<EnglishGame.Dtos.SpeakingFeedback> EvaluateSpeakingAsync(
        string topic, string level, string partId, string prompt, string userTranscript, CancellationToken ct);
}