using Microsoft.Extensions.AI;

namespace AcademyWhatsAppBot.Tests.Unit;

/// <summary>
/// Stand-in for a language model: returns a canned answer, throws, or waits, and records what it received.
/// </summary>
public sealed class FakeChatClient(Func<CancellationToken, Task<string>> respond) : IChatClient
{
    public int Calls { get; private set; }

    public ChatOptions? LastOptions { get; private set; }

    public static FakeChatClient Returning(string json) => new(_ => Task.FromResult(json));

    public static FakeChatClient Throwing(Exception exception) => new(_ => Task.FromException<string>(exception));

    public static FakeChatClient Hanging() => new(async token =>
    {
        await Task.Delay(Timeout.Infinite, token);
        return string.Empty;
    });

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Calls++;
        LastOptions = options;
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, await respond(cancellationToken)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
