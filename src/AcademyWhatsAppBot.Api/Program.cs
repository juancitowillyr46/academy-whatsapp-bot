using AcademyWhatsAppBot.Api.Balance;
using AcademyWhatsAppBot.Api.Data;
using AcademyWhatsAppBot.Api.Processing;
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

// Data:FilePath is relative to the API project folder, so it works from any working directory.
string ResolveDataPath(DataOptions options) =>
    Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, options.FilePath));

builder.Services
    .AddOptions<DataOptions>()
    .Bind(builder.Configuration.GetSection(DataOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => File.Exists(ResolveDataPath(options)), "Data:FilePath does not point to an existing file.")
    .ValidateOnStart();

builder.Services.AddSingleton<IStudentRepository>(services =>
    new JsonStudentRepository(ResolveDataPath(services.GetRequiredService<IOptions<DataOptions>>().Value)));

builder.Services.AddSingleton<BalanceService>();

// The client is used by a long-lived worker, so recycle pooled connections to pick up DNS changes.
builder.Services
    .AddHttpClient<IWhatsAppClient, WhatsAppClient>(client => client.BaseAddress = new Uri("https://graph.facebook.com/"))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<MessageQueue>();
builder.Services.AddHostedService<MessageWorker>();

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
