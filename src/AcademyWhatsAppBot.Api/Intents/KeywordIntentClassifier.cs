using AcademyWhatsAppBot.Api.Common;

namespace AcademyWhatsAppBot.Api.Intents;

/// <summary>
/// Rule-based classifier: free, instant and predictable. Detects balance keywords ignoring case, accents and extra spaces.
/// </summary>
public sealed class KeywordIntentClassifier : IIntentClassifier
{
    /// <summary>Normalized keywords (lowercase, no accents) that mean a balance request.</summary>
    public static readonly string[] BalanceKeywords = ["saldo", "cuanto debo", "deuda"];

    public Task<Intent> ClassifyAsync(string? text, CancellationToken cancellationToken = default) =>
        Task.FromResult(Classify(text));

    public static Intent Classify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Intent.Unknown;
        }

        var normalized = TextNormalizer.Normalize(text);
        return BalanceKeywords.Any(normalized.Contains) ? Intent.Balance : Intent.Unknown;
    }
}
