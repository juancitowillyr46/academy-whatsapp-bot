namespace AcademyWhatsAppBot.Api.Data;

/// <summary>
/// A student and the guardian who pays. Mapped from snake_case JSON columns (student_id, guardian_phone...).
/// </summary>
/// <param name="Balance">Amount due in COP; 0 means up to date.</param>
public sealed record Student(
    string StudentId,
    string StudentName,
    string GuardianName,
    string GuardianPhone,
    string Period,
    decimal Balance,
    DateOnly DueDate,
    string PaymentLink,
    bool IsActive);
