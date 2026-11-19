namespace AcademyWhatsAppBot.Api.Replies;

/// <summary>
/// All user-facing WhatsApp texts (Spanish). Code, logs and identifiers stay in English.
/// </summary>
public static class Messages
{
    public static string StatementHeader(string guardianName) =>
        $"Hola {guardianName} 👋\nEste es el estado de cuenta:";

    public static string StudentWithBalance(string studentName, string period, string amount, string dueDate, string paymentLink) =>
        $"• {studentName} – {period}: {amount} (vence el {dueDate})\n  Paga aquí: {paymentLink}";

    public static string StudentUpToDate(string studentName) =>
        $"• {studentName} – al día ✅";

    public const string PaymentFooter = "Si ya pagaste, envía el comprobante por este chat.";

    public const string PhoneNotRegistered =
        "No encontramos tu número registrado en la academia.\nPor favor comunícate con la administración para actualizar tus datos.";

    public const string Help =
        "Hola 👋 Escribe *saldo* para consultar tu estado de cuenta.\nSi necesitas otra cosa, en breve te atiende una persona.";
}
