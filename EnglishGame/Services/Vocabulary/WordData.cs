namespace EnglishGame.Services.Vocabulary;

public class WordData
{
    public string Word { get; set; } = "";
    public string PartOfSpeech { get; set; } = "";
    public string Definition { get; set; } = "";
    public string Example { get; set; } = "";
    public List<string> Synonyms { get; set; } = new();
    public List<string> Antonyms { get; set; } = new();
}
