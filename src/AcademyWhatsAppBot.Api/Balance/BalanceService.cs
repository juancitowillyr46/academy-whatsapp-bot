using AcademyWhatsAppBot.Api.Common;
using AcademyWhatsAppBot.Api.Data;
using AcademyWhatsAppBot.Api.Processing;

namespace AcademyWhatsAppBot.Api.Balance;

/// <summary>
/// Decides the reply for an incoming message: balance statement, not registered, or help.
/// </summary>
public sealed class BalanceService(IStudentRepository students)
{
    public async Task<string> BuildReplyAsync(IncomingMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsBalanceRequest(message.Text))
        {
            return Messages.Help;
        }

        var guardianStudents = await students.FindActiveByGuardianPhoneAsync(message.From, cancellationToken);
        if (guardianStudents.Count == 0)
        {
            return Messages.PhoneNotRegistered;
        }

        return BuildStatement(guardianStudents);
    }

    /// <summary>
    /// True if the text contains a balance keyword, ignoring case, accents and extra spaces.
    /// </summary>
    public static bool IsBalanceRequest(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = TextNormalizer.Normalize(text);
        return Messages.BalanceKeywords.Any(normalized.Contains);
    }

    private static string BuildStatement(IReadOnlyList<Student> students)
    {
        var lines = new List<string> { Messages.StatementHeader(students[0].GuardianName), string.Empty };

        foreach (var student in students)
        {
            lines.Add(student.Balance > 0
                ? Messages.StudentWithBalance(
                    student.StudentName,
                    student.Period,
                    Formatters.Currency(student.Balance),
                    Formatters.Date(student.DueDate),
                    student.PaymentLink)
                : Messages.StudentUpToDate(student.StudentName));
        }

        if (students.Any(student => student.Balance > 0))
        {
            lines.Add(string.Empty);
            lines.Add(Messages.PaymentFooter);
        }

        return string.Join('\n', lines);
    }
}
