using System.Text.Json;

using Microsoft.Extensions.AI;

namespace AcademyWhatsAppBot.Api.Intents;

/// <summary>
/// Classifies free-form messages with a language model behind <see cref="IChatClient"/> (Ollama today, any provider later).
/// The model must answer with a JSON object constrained to the <see cref="Intent"/> values, so it can only pick
/// a category: it never writes the reply, amounts or names.
/// </summary>
public sealed class LlmIntentClassifier(IChatClient chatClient) : IIntentClassifier
{
    private const string SystemPrompt = """
        Clasificas mensajes de WhatsApp que padres de familia envían a una academia deportiva en Colombia.
        Devuelve solo la intención:
        - balance: pregunta por su saldo, deuda, cuánto debe, pagos pendientes, montos o fecha de vencimiento de SU cuenta o de SUS hijos.
        - handoff: dice que ya pagó, envía o menciona un comprobante, tiene un reclamo, quiere hablar con una persona, o pide algo administrativo que un bot no resuelve.
        - greeting: solo saluda o agradece.
        - off_topic: cualquier otra cosa sin relación con la academia.

        Ejemplos:
        "me pasas el estado de cuenta de mi hija" -> balance
        "cuánto tengo que pagar de la mensualidad" -> balance
        "hasta qué fecha puedo pagar" -> balance
        "cuánto debe mi hijo Samuel" -> balance
        "ya hice la consignación ayer" -> handoff
        "necesito un certificado de la academia" -> handoff
        "buenos días" -> greeting
        "recomiéndame una película" -> off_topic
        Cualquier pregunta sobre montos, deudas, pagos pendientes o fechas de pago es balance, aunque nombre a una persona.
        """;

    // Wire values the model may return, mapped to the enum.
    private static readonly Dictionary<string, Intent> IntentsByName = new()
    {
        ["balance"] = Intent.Balance,
        ["handoff"] = Intent.Handoff,
        ["greeting"] = Intent.Greeting,
        ["off_topic"] = Intent.OffTopic,
    };

    private static readonly ChatOptions Options = new()
    {
        Temperature = 0,
        ResponseFormat = ChatResponseFormat.ForJsonSchema(
            JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new { intent = new { type = "string", @enum = IntentsByName.Keys.ToArray() } },
                required = new[] { "intent" },
            }),
            schemaName: "intent"),
    };

    public async Task<Intent> ClassifyAsync(string? text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Intent.Unknown;
        }

        var response = await chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.System, SystemPrompt), new ChatMessage(ChatRole.User, text)],
            Options,
            cancellationToken);

        return ParseIntent(response.Text);
    }

    /// <summary>
    /// Maps the model's JSON answer to an intent; anything unexpected is <see cref="Intent.Unknown"/>.
    /// </summary>
    public static Intent ParseIntent(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("intent", out var value)
                && value.ValueKind == JsonValueKind.String
                && IntentsByName.TryGetValue(value.GetString()!, out var intent)
                ? intent
                : Intent.Unknown;
        }
        catch (JsonException)
        {
            return Intent.Unknown;
        }
    }
}
