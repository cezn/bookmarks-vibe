using System.Threading.Channels;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerApp;

public class KafkaBackgroundService(
    ILogger<KafkaBackgroundService> logger,
    IConsumer<string, byte[]> consumer,
    KafkaMessageWorkerLoop workerLoop,
    [FromKeyedServices("KafkaConsumerAppHandlers")] Dictionary<string, Dictionary<string, object>> handlers
) : BackgroundService
{
    private const int WorkerCount = 4;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogStarting();
        return Task.Run(() => Consume(stoppingToken), stoppingToken);
    }

    public async Task Consume(CancellationToken ct)
    {
        consumer.Subscribe(handlers.Keys);

        var processedChannel = CreateProcessedItemChannel();
        var workerChannels = CreateWorkerChannels();
        var workerTasks = StartWorkers(processedChannel, workerChannels, ct);

        while (!ct.IsCancellationRequested)
        {
            ConsumeResult<string, byte[]>? result = null;
            try
            {
                // TODO: should consumer commit offsets on partition revoke?
                DrainProcessed(processedChannel.Reader, ct);
                result = consumer.Consume(TimeSpan.FromMilliseconds(250));
                if (result is null || result.IsPartitionEOF)
                    continue;

                consumer.Pause([result.TopicPartition]);
                var workerIndex = Math.Abs(result.Partition.Value) % WorkerCount;
                await workerChannels[workerIndex].Writer.WriteAsync(result, ct);
            }
            catch (KafkaException kex) when (kex.Error.Code == ErrorCode.Local_State)
            {
                // error during 'StoreOffset' on a message for which the consumer is not assigned anymore
                logger.LogWarning(kex, "Kafka consumer in invalid state: {Message}", kex.Message);
                continue;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogErrorProcessingMessage(ex);
                if (result is not null)
                    consumer.Seek(result.TopicPartitionOffset);

                await Task.Delay(5000, ct);
            }
        }

        foreach (var q in workerChannels)
            q.Writer.TryComplete();

        try
        {
            await Task.WhenAll(workerTasks);
        }
        catch
        {
            // Ignore worker failures during shutdown.
        }

        // Best-effort drain to store offsets for already processed messages.
        try
        {
            DrainProcessed(processedChannel.Reader, ct);
        }
        catch
        {
            // Ignore shutdown errors.
        }

        consumer.Close();
        consumer.Dispose();
    }

    private Task[] StartWorkers(
        Channel<ProcessedItem> processed,
        Channel<ConsumeResult<string, byte[]>>[] workerQueues,
        CancellationToken ct
    ) =>
        workerQueues
            .Select((q, i) => Task.Run(() => workerLoop.RunAsync(q.Reader, processed.Writer, ct), ct))
            .ToArray();

    private static Channel<ConsumeResult<string, byte[]>>[] CreateWorkerChannels() =>
        Enumerable
            .Range(0, WorkerCount)
            .Select(_ =>
                Channel.CreateBounded<ConsumeResult<string, byte[]>>(
                    new BoundedChannelOptions(256)
                    {
                        FullMode = BoundedChannelFullMode.Wait,
                        SingleReader = true,
                        SingleWriter = true,
                    }
                )
            )
            .ToArray();

    private static Channel<ProcessedItem> CreateProcessedItemChannel() =>
        Channel.CreateUnbounded<ProcessedItem>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        );

    private void DrainProcessed(ChannelReader<ProcessedItem> reader, CancellationToken ct)
    {
        while (reader.TryRead(out var processed))
        {
            try
            {
                // Always attempt to resume the partition so it can be consumed again.
                consumer.Resume([processed.Result.TopicPartition]);

                switch (processed.Outcome)
                {
                    case ProcessingOutcome.StoreOffset:
                        consumer.StoreOffset(processed.Result);
                        break;
                    case ProcessingOutcome.Seek:
                        consumer.Seek(processed.Result.TopicPartitionOffset);
                        break;
                    default:
                        break;
                }
            }
            catch (KafkaException kex) when (kex.Error.Code == ErrorCode.Local_State)
            {
                logger.LogWarning(kex, "Kafka consumer in invalid state: {Message}", kex.Message);
            }
        }
    }
}
