using System.Collections.Concurrent;

using AcademyWhatsAppBot.Api.WhatsApp;

namespace AcademyWhatsAppBot.Tests.Integration;

/// <summary>
/// Records the messages the bot would have sent instead of calling Meta.
/// </summary>
public sealed class FakeWhatsAppClient : IWhatsAppClient
{
    private readonly ConcurrentQueue<SentMessage> _sent = new();
    private readonly SemaphoreSlim _signal = new(0);

    public IReadOnlyList<SentMessage> Sent => [.. _sent];

    public Task<bool> SendTextMessageAsync(string to, string text, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentMessage(to, text));
        _signal.Release();
        return Task.FromResult(true);
    }

    /// <summary>
    /// Waits until <paramref name="count"/> messages have been sent by the background worker.
    /// </summary>
    public async Task<IReadOnlyList<SentMessage>> WaitForMessagesAsync(int count, TimeSpan? timeout = null)
    {
        for (var i = 0; i < count; i++)
        {
            if (!await _signal.WaitAsync(timeout ?? TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException($"Expected {count} sent message(s) but got {_sent.Count}.");
            }
        }

        return Sent;
    }
}

public sealed record SentMessage(string To, string Text);
