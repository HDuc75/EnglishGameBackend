namespace EnglishGame.Services.Vocabulary;

/// <summary>
/// Curated CEFR A2 and B1 vocabulary lists organized by topic keywords.
/// Words are matched to topics by substring — e.g. topic "technology" picks from TechWords.
/// </summary>
public static class CefrWordList
{
    // ── A2 words ───────────────────────────────────────────────────────────────
    private static readonly string[] A2General = new[]
    {
        "able", "above", "accept", "across", "act", "active", "activity",
        "actually", "address", "adult", "afraid", "after", "afternoon", "again",
        "age", "ago", "agree", "air", "all", "allow", "already", "also",
        "always", "angry", "animal", "another", "answer", "apply", "area",
        "arrive", "ask", "baby", "back", "bad", "bag", "bank", "beautiful",
        "because", "become", "before", "begin", "believe", "better", "between",
        "big", "bill", "body", "book", "born", "both", "break", "bring",
        "brother", "build", "bus", "busy", "call", "camera", "capital",
        "careful", "carry", "catch", "cause", "change", "cheap", "check",
        "child", "clean", "clear", "clever", "clock", "close", "clothes",
        "cold", "colour", "come", "comfortable", "common", "cook", "cool",
        "correct", "cost", "country", "course", "cover", "cross", "culture",
        "dark", "date", "daughter", "decide", "delete", "describe", "different",
        "difficult", "dinner", "direct", "discuss", "distance", "door", "down",
        "draw", "dream", "drink", "drive", "early", "easy", "enjoy", "enter",
        "environment", "equal", "escape", "every", "exam", "example", "exist",
        "expensive", "explain", "fall", "family", "famous", "fast", "feel",
        "finish", "follow", "forget", "friend", "full", "future", "game",
        "garden", "give", "glad", "great", "group", "grow", "guess", "happy",
        "hard", "hate", "have", "help", "here", "high", "history", "home",
        "hope", "hotel", "hour", "house", "huge", "idea", "important",
        "include", "increase", "inside", "invite", "join", "just", "keep",
        "kind", "know", "large", "last", "later", "learn", "leave", "letter",
        "level", "light", "like", "listen", "little", "live", "local", "long",
        "look", "lose", "love", "lunch", "make", "many", "market", "meal",
        "meet", "message", "mind", "miss", "modern", "money", "month", "move",
        "music", "national", "natural", "need", "news", "next", "nice",
        "often", "open", "order", "other", "outside", "own", "park", "part",
        "pass", "past", "people", "perfect", "phone", "photo", "place", "plan",
        "plant", "play", "police", "popular", "prepare", "price", "problem",
        "programme", "public", "put", "question", "quick", "quiet", "ready",
        "real", "reason", "remember", "return", "right", "road", "room",
        "round", "rule", "safe", "same", "save", "school", "send", "sentence",
        "share", "shop", "show", "simple", "since", "sing", "sister", "sleep",
        "slow", "small", "smart", "smile", "some", "soon", "speak", "spend",
        "sport", "start", "station", "stay", "still", "stop", "store",
        "story", "street", "student", "study", "subject", "suggest", "sure",
        "swim", "teach", "team", "thank", "think", "today", "together",
        "tomorrow", "traffic", "train", "travel", "true", "turn", "type",
        "understand", "until", "usually", "visit", "wait", "walk", "want",
        "watch", "weather", "week", "well", "without", "work", "world",
        "write", "year", "young"
    };

    private static readonly string[] A2Tech = new[]
    {
        "app", "call", "camera", "channel", "charge", "chat", "click",
        "computer", "connect", "data", "delete", "device", "digital",
        "download", "email", "file", "game", "internet", "keyboard",
        "laptop", "link", "message", "mobile", "mouse", "network",
        "online", "password", "phone", "photo", "print", "programme",
        "screen", "search", "send", "share", "signal", "site", "software",
        "tablet", "text", "upload", "video", "website", "wifi"
    };

    private static readonly string[] A2Environment = new[]
    {
        "air", "animal", "beach", "clean", "cloud", "cold", "country",
        "earth", "energy", "farm", "flower", "forest", "fresh", "garden",
        "grass", "green", "grow", "heat", "hill", "hot", "island", "lake",
        "land", "leaf", "light", "mountain", "natural", "ocean", "park",
        "plant", "rain", "recycle", "river", "rock", "sea", "season",
        "sky", "snow", "soil", "sun", "tree", "water", "weather", "wind"
    };

    // ── B1 words ───────────────────────────────────────────────────────────────
    private static readonly string[] B1General = new[]
    {
        "ability", "absence", "achieve", "advantage", "advertise", "affect",
        "aggressive", "ambitious", "analyse", "announce", "appropriate",
        "argue", "atmosphere", "attach", "attitude", "average", "avoid",
        "aware", "behaviour", "benefit", "capable", "character", "citizen",
        "claim", "communicate", "community", "compare", "compete", "concern",
        "confident", "confusion", "consider", "contribute", "convenient",
        "creative", "crime", "crisis", "criticism", "current", "damage",
        "debate", "decrease", "defend", "definition", "demand", "demonstrate",
        "deny", "describe", "design", "despite", "develop", "difficult",
        "discover", "display", "diverse", "divide", "doubt", "economy",
        "effect", "effective", "effort", "emotion", "emphasise", "encourage",
        "engage", "enormous", "enterprise", "establish", "evaluate", "evidence",
        "examine", "experience", "facility", "failure", "feature", "focus",
        "freedom", "frequent", "fundamental", "generate", "global", "graduate",
        "guarantee", "manage", "identify", "ignore", "imagine", "impact",
        "improve", "income", "indicate", "influence", "inform", "inspire",
        "intelligence", "intend", "introduce", "invest", "judge", "legal",
        "lifestyle", "likely", "limit", "manage", "method", "minimum",
        "monitor", "motivation", "negative", "negotiate", "normal", "obtain",
        "occupy", "opportunity", "outcome", "participate", "perform", "positive",
        "practical", "predict", "prefer", "prevent", "principle", "process",
        "profession", "progress", "promote", "propose", "protect", "provide",
        "purpose", "qualify", "react", "realise", "recognise", "reduce",
        "refer", "reflect", "relationship", "release", "relevant", "rely",
        "research", "resource", "respond", "responsible", "restore", "reveal",
        "role", "schedule", "significant", "situation", "society", "solve",
        "source", "specific", "strategy", "structure", "succeed", "sufficient",
        "suggest", "support", "survive", "technology", "theory", "traditional",
        "transfer", "transform", "transport", "trend", "unique", "vary",
        "vision", "volunteer", "whereas"
    };

    private static readonly string[] B1Tech = new[]
    {
        "algorithm", "artificial", "automation", "bandwidth", "blockchain",
        "browser", "cloud", "coding", "cybersecurity", "database",
        "encryption", "firewall", "hardware", "infrastructure", "innovation",
        "interface", "malware", "navigate", "platform", "privacy",
        "processor", "programming", "protocol", "server", "simulation",
        "software", "storage", "streaming", "upgrade", "virtual", "wireless"
    };

    private static readonly string[] B1Environment = new[]
    {
        "biodiversity", "carbon", "climate", "conservation", "contaminate",
        "deforestation", "ecosystem", "emission", "endangered", "erosion",
        "extinction", "fossil", "greenhouse", "habitat", "hazardous",
        "humidity", "legislation", "ozone", "pesticide", "pollution",
        "precipitation", "renewable", "resource", "sustainability",
        "temperature", "toxic", "urban", "vegetation", "vulnerable", "wildlife"
    };

    // ── Public API ─────────────────────────────────────────────────────────────

    public static IReadOnlyList<string> GetWords(string level, string topic, int count)
    {
        var pool = BuildPool(level, topic);

        // Shuffle deterministically within randomness
        var rng = new Random();
        return pool.OrderBy(_ => rng.Next()).Take(Math.Min(count, pool.Count)).ToList();
    }

    private static List<string> BuildPool(string level, string topic)
    {
        var topicLower = (topic ?? "").ToLowerInvariant();
        var isTech = topicLower.Contains("tech") || topicLower.Contains("computer") || topicLower.Contains("digital") || topicLower.Contains("internet");
        var isEnv = topicLower.Contains("env") || topicLower.Contains("nature") || topicLower.Contains("climate") || topicLower.Contains("wildlife");

        var pool = new List<string>();

        if (level.Equals("A2", StringComparison.OrdinalIgnoreCase) || level.Equals("0", StringComparison.OrdinalIgnoreCase))
        {
            pool.AddRange(A2General);
            if (isTech) pool.AddRange(A2Tech);
            if (isEnv) pool.AddRange(A2Environment);
        }
        else
        {
            pool.AddRange(B1General);
            if (isTech) pool.AddRange(B1Tech);
            if (isEnv) pool.AddRange(B1Environment);
        }

        return pool.Distinct().ToList();
    }
}
