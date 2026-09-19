using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerApp.Dlq;

public interface IDeadLetterPublisher
{
    Task<bool> TryPublishDeadLetter(
        ConsumeResult<string, byte[]> message,
        string reason,
        string? type,
        CancellationToken ct
    );

    /// <summary>
    /// Publishes a blocked-user state change and returns the assigned Kafka offset, which is the
    /// monotonic per-key version for the compacted state topic. Returns <c>null</c> on failure.
    /// </summary>
    Task<long?> TryPublishBlockedUserState(string userId, bool blocked, string reason, CancellationToken ct);
}

public sealed class KafkaDeadLetterPublisher(
    IConfiguration config,
    KafkaDlqOptions options,
    ILogger<KafkaDeadLetterPublisher> logger,
    [FromKeyedServices("dlq-producer")] IProducer<string, byte[]> producer
) : IDeadLetterPublisher, IDisposable
{
    public async Task<bool> TryPublishDeadLetter(
        ConsumeResult<string, byte[]> message,
        string reason,
        string? type,
        CancellationToken ct
    )
    {
        if (!options.Enabled)
            return true;

        try
        {
            await producer.ProduceAsync(options.Topic, CreateDeadLetterMessage(message, reason, type), ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish DLQ message for topic {Topic} and offset {Offset}",
                message.Topic,
                message.Offset.Value
            );
            return false;
        }
    }

    private static Message<string, byte[]> CreateDeadLetterMessage(
        ConsumeResult<string, byte[]> message,
        string reason,
        string? type
    )
    {
        var headers = CloneHeaders(message.Message.Headers);
        headers.Add("x-dlq-reason", Encoding.UTF8.GetBytes(reason));
        headers.Add("x-dlq-original-topic", Encoding.UTF8.GetBytes(message.Topic));
        headers.Add("x-dlq-original-partition", Encoding.UTF8.GetBytes(message.Partition.Value.ToString()));
        headers.Add("x-dlq-original-offset", Encoding.UTF8.GetBytes(message.Offset.Value.ToString()));
        headers.Add("x-dlq-created-at", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")));
        if (!string.IsNullOrWhiteSpace(type))
            headers.Add("x-dlq-original-type", Encoding.UTF8.GetBytes(type));

        var dlqMessage = new Message<string, byte[]>
        {
            Key = message.Message.Key,
            Value = message.Message.Value,
            Headers = headers,
            Timestamp = message.Message.Timestamp,
        };
        return dlqMessage;
    }

    public async Task<long?> TryPublishBlockedUserState(string userId, bool blocked, string reason, CancellationToken ct)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(userId))
            return null;

        try
        {
            var result = await producer.ProduceAsync(
                options.BlockedUsersTopic,
                CreateMessageForUserBlock(userId, blocked, reason),
                ct
            );
            return result.Offset.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish blocked user state for user {UserId}", userId);
            return null;
        }
    }

    private static Message<string, byte[]> CreateMessageForUserBlock(string userId, bool blocked, string reason)
    {
        return new Message<string, byte[]>
        {
            Key = userId,
            Value = blocked ? "1"u8.ToArray() : "0"u8.ToArray(),
            Headers =
            [
                new Header("x-user-blocked", blocked ? "1"u8.ToArray() : "0"u8.ToArray()),
                new Header("x-block-reason", Encoding.UTF8.GetBytes(reason)),
                new Header("x-blocked-updated-at", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O"))),
            ],
        };
    }

    public void Dispose()
    {
        producer.Flush(TimeSpan.FromSeconds(5));
        producer.Dispose();
    }

    private static Headers CloneHeaders(Headers headers)
    {
        var cloned = new Headers();
        foreach (var header in headers)
            cloned.Add(header.Key, header.GetValueBytes());

        return cloned;
    }
}
