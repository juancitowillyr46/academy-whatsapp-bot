namespace AcademyWhatsAppBot.Api.Intents;

/// <summary>
/// What the parent wants. The reply text is always built by code from the intent, never written by a model.
/// </summary>
public enum Intent
{
    /// <summary>Not understood, or the classifier failed; answered with the help text.</summary>
    Unknown,

    /// <summary>
    /// Asks about amounts, debts or due dates. Also used when they name another person: the reply only ever
    /// contains the sender's own students, so no separate "other family" intent is needed.
    /// </summary>
    Balance,

    /// <summary>Says they already paid, sends a receipt, complains or asks for a person.</summary>
    Handoff,

    /// <summary>Only greets or thanks.</summary>
    Greeting,

    /// <summary>Unrelated to the academy.</summary>
    OffTopic,
}
