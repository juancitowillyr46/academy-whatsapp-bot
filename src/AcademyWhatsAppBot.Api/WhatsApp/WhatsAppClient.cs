using System.Net.Http.Headers;
using System.Text.Json;

using AcademyWhatsAppBot.Api.Common;

using Microsoft.Extensions.Options;

namespace AcademyWhatsAppBot.Api.WhatsApp;

/// <summary>
/// Sends messages through the WhatsApp Cloud API (Graph API /{phone-number-id}/messages).
/// </summary>
public sealed class WhatsAppClient(HttpClient httpClient, IOptions<WhatsAppOptions> options, ILogger<WhatsAppClient> logger)
    : IWhatsAppClient
{
    private readonly WhatsAppOptions _options = options.Value;

    public async Task<bool> SendTextMessageAsync(string to, string text, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            messaging_product = "whatsapp",
            to,
            type = "text",
            text = new { body = text },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.GraphApiVersion}/{_options.PhoneNumberId}/messages")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Message sent to {To}", PhoneNormalizer.Mask(to));
            return true;
        }

        // Most demo failures show up here: expired token (190), recipient not in test list (131030), etc.
        var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var (code, message) = ParseError(errorJson);
        logger.LogError(
            "WhatsApp API error {StatusCode} sending to {To}: code {ErrorCode} – {ErrorMessage}",
            (int)response.StatusCode,
            PhoneNormalizer.Mask(to),
            code,
            message);
        return false;
    }

    private static (int? Code, string? Message) ParseError(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                int? code = error.TryGetProperty("code", out var c) && c.TryGetInt32(out var value) ? value : null;
                var message = error.TryGetProperty("message", out var m) ? m.GetString() : null;
                return (code, message);
            }
        }
        catch (JsonException)
        {
            // Not JSON; fall through.
        }

        return (null, json);
    }
}
