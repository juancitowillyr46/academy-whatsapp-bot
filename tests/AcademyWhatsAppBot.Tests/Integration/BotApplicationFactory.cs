using System.Security.Cryptography;
using System.Text;

using AcademyWhatsAppBot.Api.WhatsApp;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AcademyWhatsAppBot.Tests.Integration;

/// <summary>
/// Runs the real API in memory with fake settings, fake sample data and a fake WhatsApp client.
/// </summary>
public sealed class BotApplicationFactory : WebApplicationFactory<Program>
{
    public const string AppSecret = "test-app-secret";
    public const string VerifyToken = "test-verify-token";

    public FakeWhatsAppClient WhatsApp { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development", so the developer's User Secrets are never loaded in tests.
        builder.UseEnvironment("Testing");

        builder.UseSetting("WhatsApp:GraphApiVersion", "v0.0");
        builder.UseSetting("WhatsApp:PhoneNumberId", "200000000000000");
        builder.UseSetting("WhatsApp:AccessToken", "test-access-token");
        builder.UseSetting("WhatsApp:VerifyToken", VerifyToken);
        builder.UseSetting("WhatsApp:AppSecret", AppSecret);
        builder.UseSetting("Data:FilePath", "../../data/students.json");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IWhatsAppClient>();
            services.AddSingleton<IWhatsAppClient>(WhatsApp);
        });
    }

    /// <summary>
    /// Builds a POST /webhook request signed like Meta does (HMAC-SHA256 of the raw body).
    /// </summary>
    public static HttpRequestMessage SignedWebhookRequest(string body, string? secret = AppSecret)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhook")
        {
            Content = new ByteArrayContent(bytes),
        };
        request.Content.Headers.ContentType = new("application/json");

        if (secret is not null)
        {
            var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), bytes);
            request.Headers.Add("X-Hub-Signature-256", "sha256=" + Convert.ToHexStringLower(hash));
        }

        return request;
    }

    public static string TextMessage(string id, string from, string text) =>
        File.ReadAllText(Path.Combine("Fixtures", "TextMessage.json"))
            .Replace("{{id}}", id)
            .Replace("{{from}}", from)
            .Replace("{{text}}", text);

    public static string StatusUpdate() => File.ReadAllText(Path.Combine("Fixtures", "StatusUpdate.json"));
}
