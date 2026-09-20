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

        var rebuilder = new BlockedUsersStateRebuilder(consumer, options.BlockedUsersTopic, ready, logger);
        rebuilder.SnapshotWatermarks(ct);

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

                // The message offset is the monotonic per-key version within its partition.
                // Passing it lets the store ignore stale/duplicate updates, so this consumer
                // and the direct mutation in KafkaDlqFacade can never leave the store in an
                // inconsistent state.
                if (blocked)
                    blockedUsersStore.Block(item.Message.Key, item.Offset.Value);
                else
                    blockedUsersStore.Unblock(item.Message.Key, item.Offset.Value);

                rebuilder.TrySignalRebuild(item.TopicPartition, item.Offset.Value);
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
}
