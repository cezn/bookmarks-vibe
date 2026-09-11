using Microsoft.Extensions.Logging;

namespace KafkaConsumerApp;

public static partial class TagsKafkaSubscriptionBackgroundServiceLogs
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Starting BookmarksEventsBackgroundService")]
    public static partial void LogStarting(this ILogger logger);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Message does not contain 'type' header, skipping message"
    )]
    public static partial void LogMissingTypeHeader(this ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Error processing message")]
    public static partial void LogErrorProcessingMessage(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Received message with null value. Skipping processing."
    )]
    public static partial void LogNullMessageValue(this ILogger logger);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Error consuming message from Kafka.")]
    public static partial void LogErrorConsumingMessage(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Consumed message from topic {Topic}, partition {Partition}, offset {Offset}"
    )]
    public static partial void LogMessageConsumed(this ILogger logger, string topic, int partition, long offset);
}

public static partial class TagsMessageProcessorLogs
{
    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Processing message of type: {Type}")]
    public static partial void LogProcessingMessage(this ILogger logger, string type);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "Unknown message type: {Type}")]
    public static partial void LogUnknownMessageType(this ILogger logger, string type);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "Message processed successfully")]
    public static partial void LogMessageProcessedSuccessfully(this ILogger logger);

    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Error,
        Message = "Error dispatching message for topic {Topic} and type {Type}"
    )]
    public static partial void LogErrorDispatchingMessage(
        this ILogger logger,
        Exception ex,
        string? topic,
        string? type
    );
}
