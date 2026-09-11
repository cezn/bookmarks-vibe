using Confluent.Kafka;

namespace KafkaConsumerApp;

internal enum ProcessingOutcome
{
    None = 0,
    StoreOffset = 1,
    Seek = 2,
}

internal sealed record ProcessedItem(ConsumeResult<string, byte[]> Result, ProcessingOutcome Outcome);
