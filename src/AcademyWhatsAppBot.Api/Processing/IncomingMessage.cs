namespace AcademyWhatsAppBot.Api.Processing;

/// <summary>
/// A message received from WhatsApp, decoupled from Meta's payload shape.
/// </summary>
/// <param name="Text">Message text, or null for non-text messages (audio, image...).</param>
public sealed record IncomingMessage(string Id, string From, string Type, string? Text);
