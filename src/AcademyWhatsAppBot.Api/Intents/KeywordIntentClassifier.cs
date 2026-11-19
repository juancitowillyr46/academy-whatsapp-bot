using System.Text.RegularExpressions;

using AcademyWhatsAppBot.Api.Common;

namespace AcademyWhatsAppBot.Api.Intents;

/// <summary>
/// Rule-based classifier: free, instant and predictable. Handles the closed, common phrases (balance keywords,
/// greetings and thanks) so the language model only sees messages that need interpretation.
/// </summary>
public sealed partial class KeywordIntentClassifier : IIntentClassifier
{
    /// <summary>Normalized keywords (lowercase, no accents) that mean a balance request anywhere in the text.</summary>
    public static readonly string[] BalanceKeywords = ["saldo", "cuanto debo", "deuda"];

    /// <summary>
    /// Whole messages (normalized, letters only) that are just a greeting or thanks. Added after the baseline eval
    /// showed the small local model misclassifying short greetings (4/9).
    /// </summary>
    public static readonly HashSet<string> GreetingPhrases =
    [
        "hola", "buenas", "buen dia", "buenos dias", "buenas tardes", "buenas noches",
        "hola buenas", "hola buen dia", "hola buenos dias", "hola buenas tardes", "hola buenas noches",
        "gracias", "muchas gracias", "mil gracias", "ok gracias", "listo gracias", "gracias por la informacion",
    ];

    public Task<Intent> ClassifyAsync(string? text, CancellationToken cancellationToken = default) =>
        Task.FromResult(Classify(text));

    public static Intent Classify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Intent.Unknown;
        }

        var normalized = TextNormalizer.Normalize(text);
        if (BalanceKeywords.Any(normalized.Contains))
        {
            return Intent.Balance;
        }

        return GreetingPhrases.Contains(LettersOnly(normalized)) ? Intent.Greeting : Intent.Unknown;
    }

    // "¡Holaaa! 🙏" → "hola": drop punctuation/emoji, collapse letters repeated 3+ times, single spaces.
    private static string LettersOnly(string normalized)
    {
        var letters = NonLetters().Replace(normalized, " ");
        letters = RepeatedLetters().Replace(letters, "$1");
        return string.Join(' ', letters.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    [GeneratedRegex(@"[^\p{L}]+")]
    private static partial Regex NonLetters();

    [GeneratedRegex(@"(\p{L})\1{2,}")]
    private static partial Regex RepeatedLetters();
}
