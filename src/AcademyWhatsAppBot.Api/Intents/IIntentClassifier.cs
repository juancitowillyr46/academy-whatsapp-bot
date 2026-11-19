namespace AcademyWhatsAppBot.Api.Intents;

public interface IIntentClassifier
{
    /// <summary>
    /// Classifies a message text. Null text (audio, image...) is <see cref="Intent.Unknown"/>.
    /// </summary>
    Task<Intent> ClassifyAsync(string? text, CancellationToken cancellationToken = default);
}
