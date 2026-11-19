using System.Text.Json;

using AcademyWhatsAppBot.Api.Common;
using AcademyWhatsAppBot.Api.WhatsApp;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AcademyWhatsAppBot.Api.Webhook;

public static class WebhookEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/webhook", VerifyWebhook);
        app.MapPost("/webhook", ReceiveWebhookAsync);
        return app;
    }

    /// <summary>
    /// Meta calls this once when the webhook URL is registered. Echo the challenge if the verify token matches.
    /// </summary>
    private static IResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge,
        IOptions<WhatsAppOptions> options,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(WebhookEndpoints));

        if (mode == "subscribe" && verifyToken == options.Value.VerifyToken && challenge is not null)
        {
            logger.LogInformation("Webhook verified by Meta");
            return Results.Text(challenge);
        }

        logger.LogWarning("Webhook verification rejected");
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Receives incoming events from Meta. Only requests signed with the App Secret are accepted.
    /// </summary>
    private static async Task<IResult> ReceiveWebhookAsync(
        HttpRequest request,
        IOptions<WhatsAppOptions> options,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(WebhookEndpoints));

        // The signature is computed over the raw bytes, so read them before deserializing.
        using var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer, cancellationToken);
        var rawBody = buffer.ToArray();

        var signature = request.Headers["X-Hub-Signature-256"].ToString();
        if (!SignatureValidator.IsValid(rawBody, signature, options.Value.AppSecret))
        {
            logger.LogWarning("Webhook rejected: invalid signature");
            return Results.Unauthorized();
        }

        WebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WebhookPayload>(rawBody, JsonOptions);
        }
        catch (JsonException)
        {
            logger.LogWarning("Webhook rejected: malformed JSON");
            return Results.BadRequest();
        }

        // Status events (sent, delivered, read) have no "messages" and are ignored.
        var messages = payload?.Entry?
            .SelectMany(entry => entry.Changes ?? [])
            .SelectMany(change => change.Value?.Messages ?? [])
            ?? [];

        foreach (var message in messages)
        {
            logger.LogInformation(
                "Incoming {Type} message {MessageId} from {From}: {Text}",
                message.Type,
                message.Id,
                PhoneNormalizer.Mask(message.From),
                message.Text?.Body);
        }

        return Results.Ok();
    }
}
