using AcademyWhatsAppBot.Api.Webhook;
using AcademyWhatsAppBot.Api.WhatsApp;

using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Fail fast at startup if any WhatsApp setting is missing.
builder.Services
    .AddOptions<WhatsAppOptions>()
    .Bind(builder.Configuration.GetSection(WhatsAppOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<IWhatsAppClient, WhatsAppClient>(client =>
    client.BaseAddress = new Uri("https://graph.facebook.com/"));

var app = builder.Build();

// Runs only after options validation passed; never log secret values.
app.Lifetime.ApplicationStarted.Register(() =>
{
    var options = app.Services.GetRequiredService<IOptions<WhatsAppOptions>>().Value;
    app.Logger.LogInformation("WhatsApp settings loaded (Graph API {GraphApiVersion})", options.GraphApiVersion);
});

app.MapGet("/", () => "academy-whatsapp-bot is running");
app.MapWebhookEndpoints();

app.Run();
