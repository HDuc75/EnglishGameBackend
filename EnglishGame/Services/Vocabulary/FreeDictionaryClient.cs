using System.Net.Http.Json;
using System.Text.Json;

namespace EnglishGame.Services.Vocabulary;

/// <summary>
/// Calls the Free Dictionary API (https://api.dictionaryapi.dev) — no API key required.
/// Rate limit: 1,000 req/hour/IP.
/// </summary>
public class FreeDictionaryClient
{
    private readonly HttpClient _http;
    private readonly ILogger<FreeDictionaryClient> _logger;

    public FreeDictionaryClient(HttpClient http, ILogger<FreeDictionaryClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Looks up a word in the Free Dictionary API.
    /// Returns null if the word is not found or the API fails.
    /// </summary>
    public async Task<WordData?> LookupAsync(string word, CancellationToken ct = default)
    {
        try
        {
            var url = $"https://api.dictionaryapi.dev/api/v2/entries/en/{Uri.EscapeDataString(word)}";
            var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("FreeDictionary: word '{Word}' not found (status {Status})", word, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
                return null;

            var entry = root[0];
            var wordText = entry.TryGetProperty("word", out var wEl) ? wEl.GetString() ?? word : word;

            string partOfSpeech = "", definition = "", example = "";
            var synonyms = new List<string>();
            var antonyms = new List<string>();

            if (entry.TryGetProperty("meanings", out var meanings) && meanings.GetArrayLength() > 0)
            {
                // Pick the first meaning
                var meaning = meanings[0];
                partOfSpeech = meaning.TryGetProperty("partOfSpeech", out var posEl) ? posEl.GetString() ?? "" : "";

                // Synonyms at meaning level
                if (meaning.TryGetProperty("synonyms", out var synEl))
                    synonyms.AddRange(synEl.EnumerateArray().Select(s => s.GetString() ?? "").Where(s => s != ""));

                if (meaning.TryGetProperty("antonyms", out var antEl))
                    antonyms.AddRange(antEl.EnumerateArray().Select(s => s.GetString() ?? "").Where(s => s != ""));

                if (meaning.TryGetProperty("definitions", out var defs) && defs.GetArrayLength() > 0)
                {
                    var firstDef = defs[0];
                    definition = firstDef.TryGetProperty("definition", out var defEl) ? defEl.GetString() ?? "" : "";
                    example = firstDef.TryGetProperty("example", out var exEl) ? exEl.GetString() ?? "" : "";

                    // Synonyms at definition level (often more specific)
                    if (firstDef.TryGetProperty("synonyms", out var defSyn))
                        synonyms.AddRange(defSyn.EnumerateArray().Select(s => s.GetString() ?? "").Where(s => s != ""));
                }
            }

            // Remove duplicates, take up to 5
            synonyms = synonyms.Distinct().Take(5).ToList();
            antonyms = antonyms.Distinct().Take(3).ToList();

            return new WordData
            {
                Word = wordText,
                PartOfSpeech = partOfSpeech,
                Definition = definition,
                Example = example,
                Synonyms = synonyms,
                Antonyms = antonyms
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FreeDictionary: failed to look up word '{Word}'", word);
            return null;
        }
    }
}
