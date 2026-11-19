using AcademyWhatsAppBot.Api.Intents;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AcademyWhatsAppBot.Tests.Unit;

public class HybridIntentClassifierTests
{
    [Fact]
    public async Task KeywordMatch_DoesNotCallTheModel()
    {
        var model = FakeChatClient.Returning("""{"intent":"off_topic"}""");

        var intent = await Create(model).ClassifyAsync("saldo");

        Assert.Equal(Intent.Balance, intent);
        Assert.Equal(0, model.Calls);
    }

    [Fact]
    public async Task UnknownToRules_IsClassifiedByTheModel()
    {
        var model = FakeChatClient.Returning("""{"intent":"balance"}""");

        var intent = await Create(model).ClassifyAsync("hasta cuándo tengo para pagar?");

        Assert.Equal(Intent.Balance, intent);
        Assert.Equal(1, model.Calls);
    }

    [Fact]
    public async Task ModelFailure_FallsBackToRules()
    {
        var model = FakeChatClient.Throwing(new HttpRequestException("Ollama is not running"));

        Assert.Equal(Intent.Unknown, await Create(model).ClassifyAsync("hasta cuándo tengo para pagar?"));
    }

    [Fact]
    public async Task SlowModel_FallsBackToRulesAfterTheTimeout()
    {
        var model = FakeChatClient.Hanging();

        var intent = await Create(model, timeoutSeconds: 1).ClassifyAsync("hasta cuándo tengo para pagar?");

        Assert.Equal(Intent.Unknown, intent);
    }

    [Fact]
    public async Task CallerCancellation_IsNotSwallowed()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Create(FakeChatClient.Hanging()).ClassifyAsync("hasta cuándo tengo para pagar?", cancellation.Token));
    }

    private static HybridIntentClassifier Create(FakeChatClient model, int timeoutSeconds = 30) => new(
        new KeywordIntentClassifier(),
        new LlmIntentClassifier(model),
        Options.Create(new AiOptions { Provider = AiProviders.Ollama, TimeoutSeconds = timeoutSeconds }),
        NullLogger<HybridIntentClassifier>.Instance);
}
