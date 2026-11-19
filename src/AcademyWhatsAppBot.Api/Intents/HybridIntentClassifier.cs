using Microsoft.Extensions.Options;

namespace AcademyWhatsAppBot.Api.Intents;

/// <summary>
/// Keyword rules first (free and instant); the language model only sees messages the rules don't understand.
/// If the model is slow or unavailable, the message falls back to the rule-based result, so the bot always answers.
/// </summary>
public sealed class HybridIntentClassifier(
    KeywordIntentClassifier keywords,
    LlmIntentClassifier model,
    IOptions<AiOptions> options,
    ILogger<HybridIntentClassifier> logger) : IIntentClassifier
{
    public async Task<Intent> ClassifyAsync(string? text, CancellationToken cancellationToken = default)
    {
        var ruleIntent = await keywords.ClassifyAsync(text, cancellationToken);
        if (ruleIntent != Intent.Unknown || string.IsNullOrWhiteSpace(text))
        {
            return ruleIntent;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));

        try
        {
            var modelIntent = await model.ClassifyAsync(text, timeout.Token);
            logger.LogInformation("Model classified message as {Intent}", modelIntent);
            return modelIntent;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Timeout, Ollama not running, model missing... never leave the parent without an answer.
            logger.LogWarning(ex, "Model classification failed; falling back to keyword rules");
            return ruleIntent;
        }
    }
}
