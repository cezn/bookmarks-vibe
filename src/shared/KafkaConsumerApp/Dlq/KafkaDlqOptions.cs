namespace KafkaConsumerApp.Dlq;

public sealed class KafkaDlqOptions
{
    public bool Enabled { get; set; } = true;
    public string Topic { get; set; } = "outbox.event.Bookmark.dlq";
    public string BlockedUsersTopic { get; set; } = "bookmark.users.blocked";
    public string ReplayHeaderName { get; set; } = "x-dlq-replay";
    public string ReplayHeaderValue { get; set; } = "true";
    public string BlockReason { get; set; } = "processing_failed";

    /// <summary>
    /// How long the main consumer waits for the blocked-users store to be rebuilt from the
    /// compacted state topic before it starts processing. Guards against the rebuild never
    /// completing (e.g. broker unavailable at startup): after this elapses the consumer proceeds
    /// with a possibly-incomplete store rather than blocking forever.
    /// </summary>
    public TimeSpan RebuildTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
