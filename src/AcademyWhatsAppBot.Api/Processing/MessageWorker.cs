using AcademyWhatsAppBot.Api.Common;
using AcademyWhatsAppBot.Api.Replies;
using AcademyWhatsAppBot.Api.WhatsApp;

using Microsoft.Extensions.Caching.Memory;

namespace AcademyWhatsAppBot.Api.Processing;

/// <summary>
/// Processes queued messages in the background so the webhook can answer Meta immediately.
/// </summary>
public sealed class MessageWorker(
    MessageQueue queue,
    IMemoryCache processedIds,
    ReplyService replyService,
    IWhatsAppClient whatsAppClient,
    ILogger<MessageWorker> logger) : BackgroundService
{
    // Meta may deliver the same message more than once; remember ids long enough to ignore retries.
    private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.ReadAllAsync(stoppingToken))
        {
            // Single consumer, so check-then-set has no race condition.
            if (processedIds.TryGetValue(message.Id, out _))
            {
                logger.LogInformation("Duplicate message {MessageId} ignored", message.Id);
                continue;
            }

            processedIds.Set(message.Id, true, DeduplicationWindow);

            try
            {
                await ProcessAsync(message, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One failing message must never stop the worker.
                logger.LogError(ex, "Failed to process message {MessageId} from {From}", message.Id, PhoneNormalizer.Mask(message.From));
            }
        }
    }

    private async Task ProcessAsync(IncomingMessage message, CancellationToken cancellationToken)
    {
        var reply = await replyService.BuildReplyAsync(message, cancellationToken);
        await whatsAppClient.SendTextMessageAsync(message.From, reply, cancellationToken);
    }
}
