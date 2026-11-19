using System.ComponentModel.DataAnnotations;

namespace AcademyWhatsAppBot.Api.Intents;

/// <summary>
/// Settings for the optional language-model intent classifier.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>"None" (keyword rules only) or "Ollama" (local model).</summary>
    [Required]
    public string Provider { get; init; } = AiProviders.None;

    /// <summary>Ollama server URL.</summary>
    [Required]
    public string Endpoint { get; init; } = "http://localhost:11434";

    /// <summary>Model name as shown by <c>ollama list</c>.</summary>
    [Required]
    public string Model { get; init; } = "qwen2.5:3b";

    /// <summary>After this, the message falls back to the rule-based reply.</summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}

public static class AiProviders
{
    public const string None = "None";
    public const string Ollama = "Ollama";
}
