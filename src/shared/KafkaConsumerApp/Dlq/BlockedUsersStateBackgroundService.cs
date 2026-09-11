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
    [FromKeyedServices("blocked-users-consumer")] IConsumer<string, byte[]> consumer,
    ILogger<BlockedUsersStateBackgroundService> logger
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
            return Task.CompletedTask;

        return Task.Run(() => ConsumeState(stoppingToken), stoppingToken);
    }

    private void ConsumeState(CancellationToken ct)
    {
        consumer.Subscribe(options.BlockedUsersTopic);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var item = consumer.Consume(TimeSpan.FromMilliseconds(250));
                if (item is null || item.IsPartitionEOF)
                    continue;

                if (string.IsNullOrWhiteSpace(item.Message.Key))
                    continue;

                var blocked =
                    item.Message.Value is not null && Encoding.UTF8.GetString(item.Message.Value).Trim() == "1";

                if (blocked)
                    blockedUsersStore.Block(item.Message.Key);
                else
                    blockedUsersStore.Unblock(item.Message.Key);
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
