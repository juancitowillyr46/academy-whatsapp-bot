using AcademyWhatsAppBot.Api.Common;
using AcademyWhatsAppBot.Api.Data;
using AcademyWhatsAppBot.Api.Intents;
using AcademyWhatsAppBot.Api.Processing;

namespace AcademyWhatsAppBot.Api.Replies;

/// <summary>
/// Builds the reply for an incoming message from its intent. All texts and amounts come from code and data,
/// so a classifier (rules or a language model) can only choose which reply is sent, never its content.
/// </summary>
public sealed class ReplyService(IIntentClassifier classifier, IStudentRepository students, ILogger<ReplyService> logger)
{
    public async Task<string> BuildReplyAsync(IncomingMessage message, CancellationToken cancellationToken = default)
    {
        var intent = await classifier.ClassifyAsync(message.Text, cancellationToken);

        switch (intent)
        {
            case Intent.Balance:
                return await BuildBalanceReplyAsync(message.From, cancellationToken);

            case Intent.Handoff:
                // POC: the log is the hand-off queue; a real deployment would notify staff (email, CRM, inbox).
                logger.LogWarning("Hand-off requested by {From} (message {MessageId})", PhoneNormalizer.Mask(message.From), message.Id);
                return Messages.Handoff;

            case Intent.Greeting:
                return Messages.Greeting;

            case Intent.OffTopic:
                return Messages.OffTopic;

            default:
                return Messages.Help;
        }
    }

    // The phone always comes from WhatsApp's sender field, so a parent can only see their own students.
    private async Task<string> BuildBalanceReplyAsync(string senderPhone, CancellationToken cancellationToken)
    {
        var guardianStudents = await students.FindActiveByGuardianPhoneAsync(senderPhone, cancellationToken);

        return guardianStudents.Count == 0
            ? Messages.PhoneNotRegistered
            : BuildStatement(guardianStudents);
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
