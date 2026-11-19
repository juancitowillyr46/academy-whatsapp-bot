using System.ComponentModel.DataAnnotations;

namespace AcademyWhatsAppBot.Api.WhatsApp;

/// <summary>
/// WhatsApp Cloud API settings. Secrets come from User Secrets locally, never from appsettings.json.
/// </summary>
public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    [Required]
    public string GraphApiVersion { get; init; } = string.Empty;

    [Required]
    public string PhoneNumberId { get; init; } = string.Empty;

    [Required]
    public string AccessToken { get; init; } = string.Empty;

    [Required]
    public string VerifyToken { get; init; } = string.Empty;

    [Required]
    public string AppSecret { get; init; } = string.Empty;
}
