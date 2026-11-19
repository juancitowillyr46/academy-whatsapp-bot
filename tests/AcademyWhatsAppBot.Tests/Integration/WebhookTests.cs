using System.Net;

using AcademyWhatsAppBot.Api.Replies;

namespace AcademyWhatsAppBot.Tests.Integration;

/// <summary>
/// End-to-end tests of the webhook: HTTP → signature → queue → worker → reply. Meta is never called.
/// Each test gets its own application, so queues and deduplication caches are not shared.
/// </summary>
public sealed class WebhookTests : IDisposable
{
    // Guardian phones from data/students.json (fake data).
    private const string MartaPhone = "573001234567";
    private const string RuizPhone = "573009876543";
    private const string UnknownPhone = "573000000000";

    private readonly BotApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public WebhookTests() => _client = _factory.CreateClient();

    [Fact]
    public async Task Verification_WithCorrectToken_ReturnsChallenge()
    {
        var response = await _client.GetAsync(
            $"/webhook?hub.mode=subscribe&hub.verify_token={BotApplicationFactory.VerifyToken}&hub.challenge=12345");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("12345", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Verification_WithWrongToken_ReturnsForbidden()
    {
        var response = await _client.GetAsync("/webhook?hub.mode=subscribe&hub.verify_token=wrong&hub.challenge=12345");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Message_WithInvalidSignature_IsRejected()
    {
        var body = BotApplicationFactory.TextMessage("wamid.BAD", MartaPhone, "saldo");

        var response = await _client.SendAsync(BotApplicationFactory.SignedWebhookRequest(body, secret: "wrong-secret"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(_factory.WhatsApp.Sent);
    }

    [Fact]
    public async Task Message_WithoutSignature_IsRejected()
    {
        var body = BotApplicationFactory.TextMessage("wamid.NOSIG", MartaPhone, "saldo");

        var response = await _client.SendAsync(BotApplicationFactory.SignedWebhookRequest(body, secret: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(_factory.WhatsApp.Sent);
    }

    [Fact]
    public async Task BalanceRequest_RepliesWithStatement()
    {
        var body = BotApplicationFactory.TextMessage("wamid.1", MartaPhone, "Hola, ¿cuál es mi saldo?");

        var response = await _client.SendAsync(BotApplicationFactory.SignedWebhookRequest(body));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sent = Assert.Single(await _factory.WhatsApp.WaitForMessagesAsync(1));
        Assert.Equal(MartaPhone, sent.To);
        Assert.Contains("Hola Marta Gómez", sent.Text);
        Assert.Contains("$120.000 (vence el 5 de diciembre)", sent.Text);
    }

    [Fact]
    public async Task Siblings_ReceiveOneMessageWithBothStudents()
    {
        await _client.SendAsync(BotApplicationFactory.SignedWebhookRequest(
            BotApplicationFactory.TextMessage("wamid.2", RuizPhone, "saldo")));

        var sent = Assert.Single(await _factory.WhatsApp.WaitForMessagesAsync(1));
        Assert.Contains("• Sofía Ruiz – Noviembre 2026: $150.000", sent.Text);
        Assert.Contains("• Mateo Ruiz – al día ✅", sent.Text);
    }

    [Fact]
    public async Task UnknownPhone_RepliesNotRegistered()
    {
        await _client.SendAsync(BotApplicationFactory.SignedWebhookRequest(
            BotApplicationFactory.TextMessage("wamid.3", UnknownPhone, "saldo")));

        var sent = Assert.Single(await _factory.WhatsApp.WaitForMessagesAsync(1));
        Assert.Equal(Messages.PhoneNotRegistered, sent.Text);
    }

    [Fact]
    public async Task OtherText_RepliesHelp()
    {
        await _client.SendAsync(BotApplicationFactory.SignedWebhookRequest(
            BotApplicationFactory.TextMessage("wamid.4", MartaPhone, "¿a qué hora es el entrenamiento?")));

        var sent = Assert.Single(await _factory.WhatsApp.WaitForMessagesAsync(1));
        Assert.Equal(Messages.Help, sent.Text);
    }

    [Fact]
    public async Task Greeting_RepliesWithIntroduction()
    {
        await _client.SendAsync(BotApplicationFactory.SignedWebhookRequest(
            BotApplicationFactory.TextMessage("wamid.6", MartaPhone, "Buenas tardes")));

        var sent = Assert.Single(await _factory.WhatsApp.WaitForMessagesAsync(1));
        Assert.Equal(Messages.Greeting, sent.Text);
    }

    [Fact]
    public async Task StatusUpdate_IsAcceptedButNotAnswered()
    {
        var statusResponse = await _client.SendAsync(BotApplicationFactory.SignedWebhookRequest(BotApplicationFactory.StatusUpdate()));

        // A real message afterwards: the queue is processed in order, so if the status had produced
        // a reply (it would be the help text) it would arrive before this statement.
        await _client.SendAsync(BotApplicationFactory.SignedWebhookRequest(
            BotApplicationFactory.TextMessage("wamid.5", MartaPhone, "saldo")));

        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var sent = Assert.Single(await _factory.WhatsApp.WaitForMessagesAsync(1));
        Assert.Contains("$120.000", sent.Text);
    }

    [Fact]
    public async Task DuplicateMessage_IsAnsweredOnce()
    {
        var duplicated = BotApplicationFactory.TextMessage("wamid.DUP", MartaPhone, "saldo");

        await _client.SendAsync(BotApplicationFactory.SignedWebhookRequest(duplicated));
        await _client.SendAsync(BotApplicationFactory.SignedWebhookRequest(duplicated));
        await _client.SendAsync(BotApplicationFactory.SignedWebhookRequest(
            BotApplicationFactory.TextMessage("wamid.AFTER", MartaPhone, "¿a qué hora es el entrenamiento?")));

        // Processed in order: [statement, help]. A processed duplicate would make it [statement, statement, help].
        var sent = await _factory.WhatsApp.WaitForMessagesAsync(2);
        Assert.Equal(2, sent.Count);
        Assert.Contains("$120.000", sent[0].Text);
        Assert.Equal(Messages.Help, sent[1].Text);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
