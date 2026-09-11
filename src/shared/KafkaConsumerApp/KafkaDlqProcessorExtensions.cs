using Confluent.Kafka;
using KafkaConsumerApp.Dlq;

namespace KafkaConsumerApp;

internal static class KafkaDlqProcessorExtensions
{
    extension(IKafkaDlqFacade dlqProcessor)
    {
        internal async Task<ProcessedItem> HandleMissingTypeHeader(
            ConsumeResult<string, byte[]> item,
            string? type,
            CancellationToken ct
        ) => await HandleFailure(dlqProcessor, item, "missing_type_header", type, ct);

        internal async Task<ProcessedItem> HandleUserIsBlocked(
            ConsumeResult<string, byte[]> item,
            string? type,
            CancellationToken ct
        ) => await HandleFailure(dlqProcessor, item, "user_is_blocked", type, ct);

        internal async Task<ProcessedItem> HandleNullMessageValue(
            ConsumeResult<string, byte[]> item,
            string? type,
            CancellationToken ct
        ) => await HandleFailure(dlqProcessor, item, "null_message_value", type, ct);

        internal async Task<ProcessedItem> HandleDispatchError(
            ConsumeResult<string, byte[]> item,
            string? type,
            CancellationToken ct
        ) => await HandleFailure(dlqProcessor, item, "dispatch_error", type, ct);

        internal async Task<ProcessedItem> HandleProcessingError(
            ConsumeResult<string, byte[]> item,
            CancellationToken ct
        ) => await HandleFailure(dlqProcessor, item, "processing_error", null, ct);

        internal async Task<ProcessedItem> HandleProtobufDeserializationError(
            ConsumeResult<string, byte[]> item,
            CancellationToken ct
        ) => await HandleFailure(dlqProcessor, item, "protobuf_deserialization_error", null, ct);

        private async Task<ProcessedItem> HandleFailure(
            ConsumeResult<string, byte[]> item,
            string reason,
            string? type,
            CancellationToken ct
        ) =>
            ToProcessedItem(
                item,
                await dlqProcessor.HandleFailureAsync(item, reason, type, item.Message.Key?.Trim(), ct)
            );

        private static ProcessedItem ToProcessedItem(
            ConsumeResult<string, byte[]> item,
            DlqProcessingOutcome outcome
        ) =>
            new(
                item,
                outcome == DlqProcessingOutcome.StoreOffset ? ProcessingOutcome.StoreOffset : ProcessingOutcome.Seek
            );
    }
}
