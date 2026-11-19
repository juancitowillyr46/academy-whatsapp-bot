namespace AcademyWhatsAppBot.Api.Webhook;

// Minimal subset of Meta's webhook payload: entry[].changes[].value.messages[].
// Only the fields this bot needs are modeled; everything else is ignored.

public sealed record WebhookPayload(List<WebhookEntry>? Entry);

public sealed record WebhookEntry(List<WebhookChange>? Changes);

public sealed record WebhookChange(string? Field, WebhookValue? Value);

public sealed record WebhookValue(List<WebhookMessage>? Messages);

public sealed record WebhookMessage(string Id, string From, string Type, WebhookText? Text);

public sealed record WebhookText(string Body);
