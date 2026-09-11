namespace KafkaConsumerApp.Dlq;

public sealed class KafkaDlqOptions
{
    public bool Enabled { get; set; } = true;
    public string Topic { get; set; } = "outbox.event.Bookmark.dlq";
    public string BlockedUsersTopic { get; set; } = "bookmark.users.blocked";
    public string ReplayHeaderName { get; set; } = "x-dlq-replay";
    public string ReplayHeaderValue { get; set; } = "true";
    public string BlockReason { get; set; } = "processing_failed";
}
