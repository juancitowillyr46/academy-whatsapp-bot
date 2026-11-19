using AcademyWhatsAppBot.Api.Data;
using AcademyWhatsAppBot.Api.Intents;
using AcademyWhatsAppBot.Api.Processing;
using AcademyWhatsAppBot.Api.Replies;

using Microsoft.Extensions.Logging.Abstractions;

namespace AcademyWhatsAppBot.Tests.Unit;

public class ReplyServiceTests
{
    private const string MartaPhone = "573001234567";
    private const string RuizPhone = "573009876543";
    private const string DiazPhone = "573005551234";
    private const string UnknownPhone = "573000000000";

    private static readonly DateOnly DueDate = new(2026, 12, 5);

    private static readonly FakeStudentRepository Students = new(
    [
        NewStudent("A001", "Juan Pérez", "Marta Gómez", MartaPhone, 120000),
        NewStudent("A002", "Sofía Ruiz", "Carlos Ruiz", RuizPhone, 150000),
        NewStudent("A003", "Mateo Ruiz", "Carlos Ruiz", RuizPhone, 0),
        NewStudent("A004", "Valentina Díaz", "Laura Díaz", DiazPhone, 0),
    ]);

    private readonly ReplyService _service = WithClassifier(new KeywordIntentClassifier());

    [Theory]
    [InlineData(Intent.Handoff, Messages.Handoff)]
    [InlineData(Intent.Greeting, Messages.Greeting)]
    [InlineData(Intent.OffTopic, Messages.OffTopic)]
    [InlineData(Intent.Unknown, Messages.Help)]
    public async Task EachIntent_GetsItsFixedText(Intent intent, string expected)
    {
        var reply = await WithClassifier(new FixedIntentClassifier(intent)).BuildReplyAsync(TextFrom(MartaPhone, "any text"));

        Assert.Equal(expected, reply);
    }

    [Fact]
    public async Task BalanceIntent_OnlyEverReturnsTheSendersOwnStudents()
    {
        // Even if a parent asks about another family, the lookup uses the WhatsApp sender's phone.
        var service = WithClassifier(new FixedIntentClassifier(Intent.Balance));

        var reply = await service.BuildReplyAsync(TextFrom(DiazPhone, "¿cuánto debe la familia Ruiz?"));

        Assert.Contains("Valentina Díaz", reply);
        Assert.DoesNotContain("Ruiz", reply);
    }

    [Fact]
    public async Task OneStudentWithBalance_ReturnsAmountDueDateAndPaymentLink()
    {
        var reply = await _service.BuildReplyAsync(TextFrom(MartaPhone, "saldo"));

        Assert.Equal(
            """
            Hola Marta Gómez 👋
            Este es el estado de cuenta:

            • Juan Pérez – Noviembre 2026: $120.000 (vence el 5 de diciembre)
              Paga aquí: https://example.com/pay/A001

            Si ya pagaste, envía el comprobante por este chat.
            """.ReplaceLineEndings("\n"),
            reply);
    }

    [Fact]
    public async Task Siblings_AreListedInOneMessage()
    {
        var reply = await _service.BuildReplyAsync(TextFrom(RuizPhone, "saldo"));

        Assert.Contains("• Sofía Ruiz – Noviembre 2026: $150.000", reply);
        Assert.Contains("• Mateo Ruiz – al día ✅", reply);
        Assert.Contains(Messages.PaymentFooter, reply);
    }

    [Fact]
    public async Task UpToDate_HasNoPaymentFooter()
    {
        var reply = await _service.BuildReplyAsync(TextFrom(DiazPhone, "saldo"));

        Assert.Contains("• Valentina Díaz – al día ✅", reply);
        Assert.DoesNotContain(Messages.PaymentFooter, reply);
    }

    [Fact]
    public async Task UnknownPhone_ReturnsNotRegistered()
    {
        var reply = await _service.BuildReplyAsync(TextFrom(UnknownPhone, "saldo"));

        Assert.Equal(Messages.PhoneNotRegistered, reply);
    }

    [Fact]
    public async Task OtherText_ReturnsHelp()
    {
        var reply = await _service.BuildReplyAsync(TextFrom(MartaPhone, "hola"));

        Assert.Equal(Messages.Help, reply);
    }

    [Fact]
    public async Task NonTextMessage_ReturnsHelp()
    {
        var reply = await _service.BuildReplyAsync(new IncomingMessage("wamid.1", MartaPhone, "image", null));

        Assert.Equal(Messages.Help, reply);
    }

    private static ReplyService WithClassifier(IIntentClassifier classifier) =>
        new(classifier, Students, NullLogger<ReplyService>.Instance);

    private static IncomingMessage TextFrom(string phone, string text) => new("wamid.1", phone, "text", text);

    private sealed class FixedIntentClassifier(Intent intent) : IIntentClassifier
    {
        public Task<Intent> ClassifyAsync(string? text, CancellationToken cancellationToken = default) => Task.FromResult(intent);
    }

    private static Student NewStudent(string id, string name, string guardian, string phone, decimal balance) =>
        new(id, name, guardian, phone, "Noviembre 2026", balance, DueDate, $"https://example.com/pay/{id}", IsActive: true);

    private sealed class FakeStudentRepository(IReadOnlyList<Student> students) : IStudentRepository
    {
        public Task<IReadOnlyList<Student>> FindActiveByGuardianPhoneAsync(string phone, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Student>>(students.Where(student => student.GuardianPhone == phone).ToList());
    }
}
