namespace EnglishGame.Services.Vocabulary;

/// <summary>
/// Picks words from the CEFR list, looks them up in the Free Dictionary API,
/// and returns enriched WordData for use in quiz generation.
/// </summary>
public class VocabularyService
{
    private readonly FreeDictionaryClient _dict;
    private readonly ILogger<VocabularyService> _logger;

    public VocabularyService(FreeDictionaryClient dict, ILogger<VocabularyService> logger)
    {
        _dict = dict;
        _logger = logger;
    }

    /// <summary>
    /// Returns <paramref name="count"/> WordData objects for the given CEFR level and topic.
    /// Words not found in the dictionary are skipped; at least 1 is guaranteed
    /// (falls back to raw word with empty metadata).
    /// </summary>
    public async Task<WordData[]> GetWordsAsync(string level, string topic, int count, CancellationToken ct)
    {
        // Pick more candidates than needed to account for API misses
        var candidates = CefrWordList.GetWords(level, topic, count * 3);

        var results = new List<WordData>();

        foreach (var word in candidates)
        {
            if (ct.IsCancellationRequested) break;
            if (results.Count >= count) break;

            var data = await _dict.LookupAsync(word, ct);

            if (data != null && !string.IsNullOrWhiteSpace(data.Definition))
            {
                results.Add(data);
            }
            else
            {
                // Log but don't fail — word might not be in dictionary
                _logger.LogDebug("VocabularyService: skipping '{Word}' (not found or empty definition)", word);
            }
        }

        // If we got nothing from the API, return raw word entries (fallback)
        if (results.Count == 0)
        {
            _logger.LogWarning("VocabularyService: no words resolved from dictionary, using raw fallback");
            results = candidates.Take(count).Select(w => new WordData { Word = w }).ToList();
        }

        return results.ToArray();
    }
}
