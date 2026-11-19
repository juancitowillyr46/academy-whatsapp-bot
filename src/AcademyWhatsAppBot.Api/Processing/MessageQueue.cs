using System.Threading.Channels;

namespace AcademyWhatsAppBot.Api.Processing;

/// <summary>
/// In-memory queue between the webhook (producer) and <see cref="MessageWorker"/> (single consumer).
/// Pending messages are lost if the app stops, which is acceptable for a local demo.
/// </summary>
public sealed class MessageQueue
{
    private readonly Channel<IncomingMessage> _channel =
        Channel.CreateUnbounded<IncomingMessage>(new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(IncomingMessage message) => _channel.Writer.TryWrite(message);

    public IAsyncEnumerable<IncomingMessage> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
