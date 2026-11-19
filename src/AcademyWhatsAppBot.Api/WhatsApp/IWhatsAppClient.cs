namespace AcademyWhatsAppBot.Api.WhatsApp;

public interface IWhatsAppClient
{
    /// <summary>
    /// Sends a free-form text message. Returns false if Meta rejected it (the error is logged).
    /// </summary>
    Task<bool> SendTextMessageAsync(string to, string text, CancellationToken cancellationToken = default);
}
