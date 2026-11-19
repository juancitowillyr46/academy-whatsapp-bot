namespace AcademyWhatsAppBot.Api.Intents;

/// <summary>
/// What the parent wants. The reply text is always built by code from the intent, never written by a model.
/// </summary>
public enum Intent
{
    /// <summary>Anything the bot does not handle; answered with the help text.</summary>
    Unknown,

    /// <summary>Asks for their own balance, debt, amount due or due date.</summary>
    Balance,
}
