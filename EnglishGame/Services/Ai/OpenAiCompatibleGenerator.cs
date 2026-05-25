// OpenAiCompatibleGenerator.cs (replace whole file)
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishGame.Models;
using EnglishGame.Dtos;
using EnglishGame.Services.Vocabulary;

namespace EnglishGame.Services.Ai;

public class OpenAiCompatibleGenerator : IAiGenerator
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    public OpenAiCompatibleGenerator(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _cfg = cfg;
    }

    public async Task<(string prompt, string rawResponse, string contentJson)> GenerateQuizAsync(
        string topic, string level, int count, CancellationToken ct)
    {
        var system = "You generate English quiz content in STRICT JSON only. Output JSON only.";
        var prompt = $$"""
Topic: {{topic}}
Level: {{level}}
Count: {{count}}

Return JSON:
{
  "type": "quiz",
  "level": "<A2|B1>",
  "topic": "<string>",
  "questions": [
    { "no": 1, "skill": "vocabulary", "format": "mcq", "question": "...", "options": ["..."], "answer": "...", "explanation": "..." }
  ]
}
Output ONLY JSON.
""";

        var reply = await ChatAsync(new List<(string role, string content)>
        {
            ("system", system),
            ("user", prompt)
        }, ct);

        var json = ExtractJson(reply);
        return (prompt, reply, json);
    }

    public async Task<(string prompt, string rawResponse, string contentJson)> GenerateSkillQuizAsync(
        SkillType skill, WordData[] words, string topic, string level, int count, CancellationToken ct)
    {
        var system = "You generate English quiz questions in STRICT JSON only. Output JSON only. No markdown fences.";
        var prompt = BuildSkillPrompt(skill, words, topic, level, count);

        var reply = await ChatAsync(new List<(string role, string content)>
        {
            ("system", system),
            ("user", prompt)
        }, ct);

        var json = ExtractJson(reply);
        return (prompt, reply, json);
    }

    public async Task<(string prompt, string rawResponse, string contentJson)> GenerateAllSkillsAtOnceAsync(
        WordData[] words, string topic, string level,
        int vocabCount, int readingCount, int listenCount, int grammarCount, int speakingCount,
        CancellationToken ct)
    {
        var system = "You are an English quiz generator. Output ONLY valid JSON with no markdown, no explanation.";
        var prompt = BuildAllSkillsPrompt(words, topic, level, vocabCount, readingCount, listenCount, grammarCount, speakingCount);

        var reply = await ChatAsync(new List<(string role, string content)>
        {
            ("system", system),
            ("user", prompt)
        }, ct);

        var json = ExtractJson(reply);
        return (prompt, reply, json);
    }

    public async Task<(string prompt, string rawResponse, string contentJson)> GenerateWritingPromptAsync(
        string topic, string level, CancellationToken ct)
    {
        var system = "You are an expert English examiner. Output ONLY valid JSON with no markdown, no explanation.";
        var prompt = $$"""
Generate a VSTEP Writing test for topic: "{{topic}}", CEFR level: {{level}}.

Output exactly the following JSON structure with no other text:
{
  "type": "writing_prompt",
  "level": "{{level}}",
  "topic": "{{topic}}",
  "task1": {
    "title": "Task 1: Email/Letter",
    "prompt": "You recently [situation]. Write a letter to [person]. In your letter, you should:\n- [point 1]\n- [point 2]\n- [point 3]\n\nWrite at least 120 words.",
    "minWords": 120
  },
  "task2": {
    "title": "Task 2: Essay",
    "prompt": "[Statement about topic]. Do you agree or disagree? Give reasons and examples.\n\nWrite at least 250 words.",
    "minWords": 250
  }
}
""";

        var reply = await ChatAsync(new List<(string role, string content)>
        {
            ("system", system),
            ("user", prompt)
        }, ct);

        var json = ExtractJson(reply);
        return (prompt, reply, json);
    }

    public async Task<(string prompt, string rawResponse, string contentJson)> GenerateSpeakingPromptAsync(
        string topic, string level, CancellationToken ct)
    {
        var system = "You are an expert English examiner. Output ONLY valid JSON with no markdown, no explanation.";
        var prompt = $$"""
Generate a VSTEP Speaking test for topic: "{{topic}}", CEFR level: {{level}}.

Output exactly the following JSON structure with no other text:
{
  "type": "speaking_prompt",
  "level": "{{level}}",
  "topic": "{{topic}}",
  "part1": [
     { "theme": "[Theme 1 related to topic]", "questions": ["[Question 1]", "[Question 2]"] },
     { "theme": "[Theme 2 related to topic]", "questions": ["[Question 1]", "[Question 2]"] }
  ],
  "part2": {
     "situation": "[A daily life situation where the user must choose one of three options]",
     "options": ["[Option A]", "[Option B]", "[Option C]"],
     "question": "Which option is best for you and why?"
  },
  "part3": {
     "topic": "[A broader topic related to the main topic]",
     "bulletPoints": ["[Point 1]", "[Point 2]", "[Point 3]"],
     "followUpQuestions": ["[Follow up question 1]"]
  }
}
""";

        var reply = await ChatAsync(new List<(string role, string content)>
        {
            ("system", system),
            ("user", prompt)
        }, ct);

        var json = ExtractJson(reply);
        return (prompt, reply, json);
    }

    public async Task<WritingFeedback> EvaluateWritingAsync(
        string topic, string level, string taskId, string taskPrompt, string userContent, int minWords, CancellationToken ct)
    {
        var system = "You are a stringent VSTEP writing examiner. Output ONLY valid JSON with no markdown, no explanation.";
        var prompt = $$"""
Evaluate the following submitted writing task based on VSTEP criteria.
Topic: "{{topic}}"
Level: {{level}}
Task Description: "{{taskPrompt}}"
Minimum Words Required: {{minWords}}

User's Submission:
\"\"\"
{{userContent}}
\"\"\"

Analyze the submission for Vocabulary, Grammar, Coherence, and Task Fulfillment. Provide a score from 0 to 10 (10 being perfect for the target level).

Output exactly the following JSON structure with no other text:
{
  "taskId": "{{taskId}}",
  "score": 0,
  "grammarFeedback": "string detailing grammar errors and corrections",
  "vocabularyFeedback": "string detailing vocabulary usage and suggestions",
  "coherenceFeedback": "string detailing paragraph structure and flow",
  "taskFulfillmentFeedback": "string detailing if they answered the prompt and met the word count",
  "overallComment": "general encouraging but critical summary"
}
""";

        var reply = await ChatAsync(new List<(string role, string content)>
        {
            ("system", system),
            ("user", prompt)
        }, ct);

        var json = ExtractJson(reply);
        try
        {
            return JsonSerializer.Deserialize<WritingFeedback>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse AI writing evaluation: {ex.Message}");
        }
    }

    public async Task<Dtos.SpeakingFeedback> EvaluateSpeakingAsync(
        string topic, string level, string partId, string prompt, string userTranscript, CancellationToken ct)
    {
        var system = "You are a stringent VSTEP speaking examiner. Output ONLY valid JSON with no markdown, no explanation.";
        var userPrompt = $$"""
Evaluate the following speaking transcript from a VSTEP exam candidate.
Topic: "{{topic}}"
Level: {{level}}
Part: {{partId}}
Prompt/Question: "{{prompt}}"

Candidate's Transcript:
\"\"\"
{{userTranscript}}
\"\"\"

Score on a scale of 0-10 for Pronunciation, Fluency, and Content Quality.

Output exactly the following JSON structure with no other text:
{
  "partId": "{{partId}}",
  "score": 0,
  "pronunciationFeedback": "string detailing pronunciation strengths/weaknesses",
  "fluencyFeedback": "string detailing fluency and naturalness",
  "contentFeedback": "string detailing how well the content addressed the prompt",
  "overallComment": "brief overall encouraging summary"
}
""";

        var reply = await ChatAsync(new List<(string role, string content)>
        {
            ("system", system),
            ("user", userPrompt)
        }, ct);

        var json = ExtractJson(reply);
        try
        {
            return JsonSerializer.Deserialize<Dtos.SpeakingFeedback>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse AI speaking evaluation: {ex.Message}");
        }
    }

    private static string BuildAllSkillsPrompt(
        WordData[] words, string topic, string level,
        int vocabCount, int readingCount, int listenCount, int grammarCount, int speakingCount)
    {
        var wordBlock = string.Join("\n", words.Take(vocabCount * 2).Select(w =>
        {
            var line = $"- \"{w.Word}\"";
            if (!string.IsNullOrWhiteSpace(w.Definition)) line += $": {w.Definition}";
            if (!string.IsNullOrWhiteSpace(w.Example))    line += $" (e.g., {w.Example})";
            return line;
        }));

        var totalCount = vocabCount + readingCount + listenCount + grammarCount + speakingCount;

        // VSTEP-style sub-section counts
        int listenSectionA = Math.Max(1, listenCount / 5);               // announcements
        int listenSectionB = Math.Max(1, listenCount * 2 / 5);           // dialogues
        int listenSectionC = listenCount - listenSectionA - listenSectionB; // lectures
        int readingPassageCount = Math.Max(1, readingCount / 5);

        return $$"""
You are an English quiz generator for VSTEP/CEFR {{level}} learners. Topic: "{{topic}}".
Generate exactly {{totalCount}} MCQ questions mixing 5 skill types as below.
Output ONE JSON object ONLY — no markdown, no extra text.

SKILL BREAKDOWN:
- {{vocabCount}} VOCABULARY questions (use the real words below)
- {{readingCount}} READING questions: create {{readingPassageCount}} separate passages (~120 words each, different sub-topics of "{{topic}}"). Give each passage a "passageId": "reading-1", "reading-2", etc. Include it in every reading question.
- {{listenCount}} LISTENING questions in 3 sections:
  ** SECTION A — Announcements ({{listenSectionA}} questions): Each announcement is a short 1-3 sentence notice. Set "section":"listening-a" and a unique "passageId" like "announcement-1" on each.
  ** SECTION B — Dialogues ({{listenSectionB}} questions): Write {{listenSectionB}} short dialogues (~60 words, use real names NOT A/B). Set "section":"listening-b" and "passageId":"dialogue-1" (or -2 etc.) on each question. Use 4 questions per dialogue if possible.
  ** SECTION C — Talks/Lectures ({{listenSectionC}} questions): Write a monologue talk (~80 words). Set "section":"listening-c" and "passageId":"lecture-1" (or -2 etc.) on each question. Use 5 questions per lecture if possible.
- {{grammarCount}} GRAMMAR questions (fill-in-blank, "section":"grammar")
- {{speakingCount}} SPEAKING questions: Each is a social situation – choose the most appropriate response. "section":"speaking"

Real vocabulary words for VOCABULARY questions:
{{wordBlock}}

Return this JSON (no fences):
{
  "type": "quiz",
  "level": "{{level}}",
  "topic": "{{topic}}",
  "questions": [
    {
      "no": 1, "skill": "vocabulary", "section": "vocabulary", "format": "mcq",
      "question": "What does 'acquire' mean?",
      "options": ["A. to lose", "B. to obtain", "C. to forget", "D. to break"],
      "answer": "B. to obtain", "explanation": "'Acquire' means to obtain or gain something."
    },
    {
      "no": 2, "skill": "reading", "section": "reading", "format": "mcq",
      "passageId": "reading-1",
      "passage": "[Passage 1] ...[full ~120-word passage on sub-topic]",
      "question": "What is the main idea?",
      "options": ["A. ...", "B. ...", "C. ...", "D. ..."], "answer": "A. ...", "explanation": "..."
    },
    {
      "no": 3, "skill": "listening", "section": "listening-a", "format": "mcq",
      "passageId": "announcement-1",
      "passage": "[Announcement] Attention passengers: the 9:15 train to City Center is delayed by 20 minutes.",
      "question": "What is the announcement about?",
      "options": ["A. ...", "B. ...", "C. ...", "D. ..."], "answer": "A. ...", "explanation": "..."
    },
    {
      "no": 4, "skill": "listening", "section": "listening-b", "format": "mcq",
      "passageId": "dialogue-1",
      "passage": "[Dialogue]\nTom: ...\nLisa: ...",
      "question": "What does Tom want?",
      "options": ["A. ...", "B. ...", "C. ...", "D. ..."], "answer": "A. ...", "explanation": "..."
    },
    {
      "no": 5, "skill": "listening", "section": "listening-c", "format": "mcq",
      "passageId": "lecture-1",
      "passage": "[Talk] Today I want to discuss...",
      "question": "What is the main topic?",
      "options": ["A. ...", "B. ...", "C. ...", "D. ..."], "answer": "A. ...", "explanation": "..."
    },
    {
      "no": 6, "skill": "grammar", "section": "grammar", "format": "mcq",
      "question": "She ___ to school every day.",
      "options": ["A. go", "B. goes", "C. going", "D. gone"], "answer": "B. goes",
      "explanation": "Third person singular present simple uses -s."
    },
    {
      "no": 7, "skill": "speaking", "section": "speaking", "format": "mcq",
      "question": "Situation: Your friend says 'I'm exhausted.' What do you say?",
      "options": ["A. Good for you!", "B. You should rest.", "C. That's funny.", "D. I don't care."],
      "answer": "B. You should rest.", "explanation": "Option B shows empathy."
    }
  ]
}
Generate ALL {{totalCount}} questions now. ONLY output JSON.
""";
    }

    // ── Skill Prompt Builders ─────────────────────────────────────────────────

    private static string BuildSkillPrompt(SkillType skill, WordData[] words, string topic, string level, int count)
        => skill switch
        {
            SkillType.Vocabulary => BuildVocabPrompt(words, topic, level, count),
            SkillType.Reading    => BuildReadingPrompt(topic, level, count),
            SkillType.Listening  => BuildListeningPrompt(topic, level, count),
            SkillType.Grammar    => BuildGrammarPrompt(topic, level, count),
            SkillType.Speaking   => BuildSpeakingPrompt(topic, level, count),
            _                    => BuildVocabPrompt(words, topic, level, count)
        };

    private static string BuildVocabPrompt(WordData[] words, string topic, string level, int count)
    {
        var wordBlock = string.Join("\n", words.Take(count * 2).Select(w =>
        {
            var line = $"- Word: \"{w.Word}\"";
            if (!string.IsNullOrWhiteSpace(w.PartOfSpeech)) line += $" ({w.PartOfSpeech})";
            if (!string.IsNullOrWhiteSpace(w.Definition))   line += $"\n  Definition: {w.Definition}";
            if (!string.IsNullOrWhiteSpace(w.Example))      line += $"\n  Example: {w.Example}";
            if (w.Synonyms.Count > 0)                       line += $"\n  Synonyms: {string.Join(", ", w.Synonyms)}";
            if (w.Antonyms.Count > 0)                       line += $"\n  Antonyms: {string.Join(", ", w.Antonyms)}";
            return line;
        }));

        return $$"""
You are an English quiz generator. Use ONLY the real word data below to create {{count}} vocabulary MCQ questions.
Each question must test one word from the list (meaning, usage, or synonym).
All questions must be appropriate for CEFR level {{level}}.

Real word data:
{{wordBlock}}

Return ONLY this JSON (no markdown):
{
  "type": "quiz",
  "level": "{{level}}",
  "topic": "{{topic}}",
  "questions": [
    {
      "no": 1,
      "skill": "vocabulary",
      "format": "mcq",
      "question": "What does 'abundant' mean?",
      "options": ["A. very rare", "B. present in large quantities", "C. expensive", "D. dangerous"],
      "answer": "B. present in large quantities",
      "explanation": "'Abundant' means existing in large quantities."
    }
  ]
}
""";
    }

    private static string BuildReadingPrompt(string topic, string level, int count) => $$"""
You are an English quiz generator. Create a short reading passage (~120 words) on the topic "{{topic}}"
at CEFR level {{level}}, then write {{count}} comprehension MCQ questions about it.
Question types: main idea, detail, vocabulary in context, inference.
Each question must include the same passage text in "passage" field.

Return ONLY this JSON (no markdown):
{
  "type": "quiz",
  "level": "{{level}}",
  "topic": "{{topic}}",
  "questions": [
    {
      "no": 1,
      "skill": "reading",
      "format": "mcq",
      "passage": "<the full passage text — same for all reading questions in this set>",
      "question": "What is the main idea of the passage?",
      "options": ["A. ...", "B. ...", "C. ...", "D. ..."],
      "answer": "A. ...",
      "explanation": "..."
    }
  ]
}
""";

    private static string BuildListeningPrompt(string topic, string level, int count) => $$"""
You are an English quiz generator. Write a short dialogue transcript (~80 words) between two people
discussing "{{topic}}" at CEFR level {{level}}. Then create {{count}} MCQ questions based on it.
Question types: who said, what happened, where/when, speaker's attitude.
Each question must include the same transcript text in "passage" field.

IMPORTANT: Use real names for the speakers (e.g., Tom, Lisa, Mark) rather than just "A" and "B".

Return ONLY this JSON (no markdown):
{
  "type": "quiz",
  "level": "{{level}}",
  "topic": "{{topic}}",
  "questions": [
    {
      "no": 1,
      "skill": "listening",
      "format": "mcq",
      "passage": "[Transcript]\nTom: ...\nLisa: ...",
      "question": "What does Tom want to do?",
      "options": ["A. ...", "B. ...", "C. ...", "D. ..."],
      "answer": "A. ...",
      "explanation": "..."
    }
  ]
}
""";

    private static string BuildGrammarPrompt(string topic, string level, int count) => $$"""
You are an English quiz generator. Create {{count}} grammar/writing MCQ questions at CEFR level {{level}}.
Context: the topic is "{{topic}}".
Mix question types: fill-in-the-blank (choose the correct word/tense), error correction, sentence transformation.
For fill-in-the-blank questions, use "___" in the question text to mark the blank.

Return ONLY this JSON (no markdown):
{
  "type": "quiz",
  "level": "{{level}}",
  "topic": "{{topic}}",
  "questions": [
    {
      "no": 1,
      "skill": "grammar",
      "format": "mcq",
      "question": "She ___ to school every day.",
      "options": ["A. go", "B. goes", "C. going", "D. gone"],
      "answer": "B. goes",
      "explanation": "Third person singular present simple uses -s."
    }
  ]
}
""";

    private static string BuildSpeakingPrompt(string topic, string level, int count) => $$"""
You are an English quiz generator following the VSTEP Speaking Part 1 (Social Interaction) format.
Create {{count}} MCQ questions at CEFR level {{level}} about the topic "{{topic}}".
Each question describes a real-life social situation and the test-taker must choose the most natural/appropriate response.
The situation context goes in the "question" field prefixed with 'Situation: '.

Return ONLY this JSON (no markdown):
{
  "type": "quiz",
  "level": "{{level}}",
  "topic": "{{topic}}",
  "questions": [
    {
      "no": 1,
      "skill": "speaking",
      "format": "mcq",
      "question": "Situation: Your colleague says 'I'm really stressed about the deadline.' What is the most appropriate response?",
      "options": [
        "A. That's your problem, not mine.",
        "B. Don't worry, I can help you organise your tasks.",
        "C. You should quit your job.",
        "D. Deadlines are not important."
      ],
      "answer": "B. Don't worry, I can help you organise your tasks.",
      "explanation": "Option B shows empathy and offers practical help, which is the most natural and appropriate workplace response."
    }
  ]
}
""";

    public async Task<string> ChatAsync(List<(string role, string content)> messages, CancellationToken ct)
    {
        var baseUrl = _cfg["Ai:BaseUrl"]?.TrimEnd('/');
        var apiKey = _cfg["Ai:ApiKey"];
        var model = _cfg["Ai:Model"] ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Ai:BaseUrl is missing.");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Ai:ApiKey is missing.");

        // Try multiple common paths until one works (avoid 405)
        var paths = new List<string>();

        var cfgPath = _cfg["Ai:ChatPath"];
        if (!string.IsNullOrWhiteSpace(cfgPath)) paths.Add(cfgPath!);

        paths.AddRange(new[]
        {
            "/v1/chat/completions",
            "/chat/completions",
            "/api/v1/chat/completions",
            "/api/chat/completions",
            "/v1/chat",
            "/chat",
            "/v1/responses",
            "/responses",
            "/api/v1/responses",
            "/api/responses"
        });

        string? lastRaw = null;
        HttpStatusCode? lastStatus = null;

        var chatBody = new
        {
            model,
            temperature = 0.7,
            messages = messages.Select(m => new { role = m.role, content = m.content }).ToArray()
        };

        foreach (var p in paths.Distinct())
        {
            var url = BuildUrl(baseUrl, p);

            // Retry tối đa 3 lần khi bị 429 TooManyRequests
            const int maxRetries = 3;
            bool triedPath = false;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var (ok, status, raw) = await PostJsonBearer(url, apiKey, chatBody, ct);

                if (ok)
                {
                    if (IsResponsesEndpoint(p)) return ExtractResponsesText(raw);
                    return ExtractChatCompletionText(raw);
                }

                lastRaw = raw;
                lastStatus = status;

                // 429: chờ theo retryDelay rồi thử lại
                if (status == HttpStatusCode.TooManyRequests)
                {
                    if (attempt < maxRetries - 1)
                    {
                        var delaySec = ExtractRetryDelay(raw);
                        await Task.Delay(TimeSpan.FromSeconds(delaySec), ct);
                        continue; // retry cùng path
                    }
                    // Hết retry -> báo lỗi rõ ràng
                    throw new InvalidOperationException(
                        $"AI rate limit (429): đã thử lại {maxRetries} lần nhưng vẫn bị giới hạn. " +
                        $"Vui lòng kiểm tra quota tại https://ai.dev/rate-limit");
                }

                // 404/405: thử path tiếp theo
                if (status == HttpStatusCode.MethodNotAllowed || status == HttpStatusCode.NotFound)
                {
                    triedPath = true;
                    break;
                }

                // Lỗi khác (401/403/400): dừng ngay
                throw new InvalidOperationException($"AI provider error: {(int)status} {status}\n{raw}");
            }

            if (!triedPath) break;
        }

        throw new InvalidOperationException($"AI provider error: {(int)(lastStatus ?? 0)} {lastStatus}\n{lastRaw}");
    }

    /// <summary>Đọc retryDelay từ Gemini 429 response body ("8s", "48.6s"...).</summary>
    private static double ExtractRetryDelay(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("details", out var details))
            {
                foreach (var detail in details.EnumerateArray())
                {
                    if (detail.TryGetProperty("retryDelay", out var delay))
                    {
                        var s = (delay.GetString() ?? "10s").TrimEnd('s');
                        if (double.TryParse(s, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var sec))
                            return Math.Clamp(sec + 2, 5, 65); // thêm 2s buffer, tối đa 65s
                    }
                }
            }
        }
        catch { }
        return 12; // mặc định 12 giây
    }

    private async Task<(bool ok, HttpStatusCode status, string raw)> PostJsonBearer(
        string url,
        string apiKey,
        object body,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);

        return (resp.IsSuccessStatusCode, resp.StatusCode, raw);
    }

    private static bool IsResponsesEndpoint(string path)
    {
        var p = path.ToLowerInvariant();
        return p.Contains("responses");
    }

    private static string BuildUrl(string baseUrl, string path)
    {
        if (!path.StartsWith("/")) path = "/" + path;
        return baseUrl + path;
    }

    private static string ExtractChatCompletionText(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }

    private static string ExtractResponsesText(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        if (root.TryGetProperty("output_text", out var ot) && ot.ValueKind == JsonValueKind.String)
            return ot.GetString() ?? "";

        if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array && output.GetArrayLength() > 0)
        {
            var first = output[0];
            if (first.TryGetProperty("content", out var contentArr) && contentArr.ValueKind == JsonValueKind.Array && contentArr.GetArrayLength() > 0)
            {
                foreach (var c in contentArr.EnumerateArray())
                {
                    if (c.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                        return text.GetString() ?? "";
                    if (c.TryGetProperty("output_text", out var t2) && t2.ValueKind == JsonValueKind.String)
                        return t2.GetString() ?? "";
                }
            }
        }

        return raw;
    }

    private static string ExtractJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var s = raw.Trim();

        if (s.StartsWith("```"))
        {
            var firstNewLine = s.IndexOf('\n');
            if (firstNewLine >= 0) s = s[(firstNewLine + 1)..];
            var lastFence = s.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0) s = s[..lastFence];
            s = s.Trim();
        }

        var i = s.IndexOf('{');
        var j = s.LastIndexOf('}');
        if (i >= 0 && j > i) return s.Substring(i, j - i + 1);

        return s;
    }
}