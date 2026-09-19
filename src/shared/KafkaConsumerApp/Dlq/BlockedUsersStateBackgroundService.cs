using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerApp.Dlq;

public sealed class BlockedUsersStateBackgroundService(
    KafkaDlqOptions options,
    IBlockedUsersStore blockedUsersStore,
    BlockedUsersStateReady ready,
    [FromKeyedServices("blocked-users-consumer")] IConsumer<string, byte[]> consumer,
    ILogger<BlockedUsersStateBackgroundService> logger
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            // Nothing to rebuild; let the main consumer start immediately.
            ready.Signal();
            return Task.CompletedTask;
        }

        return Task.Run(() => ConsumeState(stoppingToken), stoppingToken);
    }

    private void ConsumeState(CancellationToken ct)
    {
        consumer.Subscribe(options.BlockedUsersTopic);

        // Snapshot the high watermark (end offset) of the state topic before consuming.
        // We don't have to worry about later messages, as our partitions are blocked until
        // we reach the watermark of blocker users.
        var watermark = SnapshotWatermark(ct);

        var rebuildSignalled = false;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var item = consumer.Consume(TimeSpan.FromMilliseconds(250));
                if (item is null)
                    continue;

                if (string.IsNullOrWhiteSpace(item.Message.Key))
                    continue;

                var blocked =
                    item.Message.Value is not null && Encoding.UTF8.GetString(item.Message.Value).Trim() == "1";

                // The message offset is the monotonic per-key version. Passing it lets the store
                // ignore stale/duplicate updates, so this consumer and the direct mutation in
                // KafkaDlqFacade can never leave the store in an inconsistent state.
                if (blocked)
                    blockedUsersStore.Block(item.Message.Key, item.Offset.Value);
                else
                    blockedUsersStore.Unblock(item.Message.Key, item.Offset.Value);

                // We have replayed every live state record that existed at startup. Signal
                // readiness once so the main consumer can start processing with an accurate
                // IsBlocked() view. Keep consuming afterwards so the store stays current with
                // new block/unblock updates.
                if (!rebuildSignalled && item.Offset.Value >= watermark)
                {
                    rebuildSignalled = true;
                    ready.Signal();
                    logger.LogInformation(
                        "Blocked-users store rebuilt from topic {Topic} (watermark {Watermark})",
                        options.BlockedUsersTopic,
                        watermark
                    );
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error consuming blocked-users state topic");
            }
        }

        consumer.Close();
    }

    /// <summary>
    /// Returns the high watermark (end offset) of the state topic's partition, i.e. the offset
    /// of the next record to be appended. Records at offsets below this value are the live state
    /// that must be replayed. The state topic is single-partition, so partition 0 is queried
    /// directly. Retries until the topic/partition is known to the broker (it may not exist yet
    /// at startup).
    /// </summary>
    private long SnapshotWatermark(CancellationToken ct)
    {
        var topicPartition = new TopicPartition(options.BlockedUsersTopic, 0);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var watermarks = consumer.QueryWatermarkOffsets(topicPartition, TimeSpan.FromSeconds(5));
                return watermarks.High.Value;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Could not snapshot watermark for topic {Topic}; retrying",
                    options.BlockedUsersTopic
                );
            }

            Task.Delay(TimeSpan.FromMilliseconds(250), ct).Wait(ct);
        }

        throw new OperationCanceledException(ct);
    }
}
