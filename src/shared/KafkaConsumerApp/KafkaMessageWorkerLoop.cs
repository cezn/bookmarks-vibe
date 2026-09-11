using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using Confluent.Kafka;
using KafkaConsumerApp.Dlq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerApp;

public class KafkaMessageWorkerLoop(
    ILogger<KafkaMessageWorkerLoop> logger,
    KafkaMessageDispatcher dispatcher,
    IKafkaDlqFacade dlqFacade,
    [FromKeyedServices("KafkaConsumerAppHandlers")] Dictionary<string, Dictionary<string, object>> handlers
)
{
    internal async Task RunAsync(
        ChannelReader<ConsumeResult<string, byte[]>> reader,
        ChannelWriter<ProcessedItem> processed,
        CancellationToken ct
    )
    {
        await foreach (var item in reader.ReadAllAsync(ct))
        {
            Activity? activity = null;
            try
            {
                var type = ExtractTypeHeader(item);
                var userId = item.Message.Key?.Trim();
                var isReplay = dlqFacade.IsReplay(item);
                activity = CreateActivity(item, ExtractActivityContext(item), type ?? "unknown");
                if (type is null)
                {
                    logger.LogMissingTypeHeader();
                    await processed.WriteAsync(await dlqFacade.HandleMissingTypeHeader(item, type, ct), ct);
                    continue;
                }

                activity?.SetTag("bookmark.message.type", type);
                logger.LogProcessingMessage(type);

                if (dlqFacade.IsBlocked(userId) && !isReplay)
                {
                    await processed.WriteAsync(await dlqFacade.HandleUserIsBlocked(item, type, ct), ct);
                    continue;
                }

                if (item.Message.Value is null)
                {
                    logger.LogNullMessageValue();
                    await processed.WriteAsync(await dlqFacade.HandleNullMessageValue(item, type, ct), ct);
                    continue;
                }

                await dispatcher.DispatchMessage(item.Topic, type, item.Message, handlers);
                logger.LogMessageProcessedSuccessfully();

                if (isReplay && !string.IsNullOrWhiteSpace(userId))
                    await dlqFacade.HandleReplaySuccessAsync(userId, ct);

                await processed.WriteAsync(new ProcessedItem(item, ProcessingOutcome.StoreOffset), ct);
            }
            catch (Google.Protobuf.InvalidProtocolBufferException pbex)
            {
                logger.LogError(pbex, "Protobuf deserialization error: {Message}", pbex.Message);
                if (activity is not null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, pbex.Message);
                    activity.AddException(pbex);
                }

                await processed.WriteAsync(await dlqFacade.HandleProtobufDeserializationError(item, ct), ct);
            }
            catch (KafkaMessageDispatchException kex)
            {
                logger.LogErrorDispatchingMessage(kex, kex.Topic, kex.Type);
                if (activity is not null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, kex.Message);
                    activity.AddException(kex);
                }

                await processed.WriteAsync(await dlqFacade.HandleDispatchError(item, kex.Type, ct), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogErrorProcessingMessage(ex);
                if (activity is not null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity.AddException(ex);
                }

                await processed.WriteAsync(await dlqFacade.HandleProcessingError(item, ct), ct);
            }
            finally
            {
                activity?.Dispose();
            }
        }
    }

    private static string? ExtractTypeHeader(ConsumeResult<string, byte[]> result) =>
        result.Message.Headers.TryGetLastBytes("type", out var typeHeader)
            ? Encoding.UTF8.GetString(typeHeader).Trim() is string { Length: > 0 } type
                ? type
                : null
            : null;

    private static Activity? CreateActivity(
        ConsumeResult<string, byte[]> result,
        ActivityContext parentContext,
        string type
    )
    {
        var activity = Monitoring.ActivitySource.StartActivity(
            $"CONSUME {result.Topic}/{type}",
            ActivityKind.Consumer,
            parentContext
        );
        activity?.SetTag("bookmark.message.key", result.Message.Key);
        activity?.SetTag("bookmark.message.topic", result.Topic);
        activity?.SetTag("bookmark.message.partition", result.Partition.Value);
        activity?.SetTag("bookmark.message.offset", result.Offset.Value);

        return activity;
    }

    private static ActivityContext ExtractActivityContext(ConsumeResult<string, byte[]> result) =>
        result.Message.Headers.TryGetLastBytes("traceparent", out var traceparentBytes)
        && ActivityContext.TryParse(Encoding.UTF8.GetString(traceparentBytes), null, out var ctx)
            ? ctx
            : default;
}
