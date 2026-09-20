using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerApp.Dlq;

/// <summary>
/// Tracks the replay of the compacted blocked-users state topic and signals readiness once every
/// partition the consumer is assigned has been replayed past its startup watermark.
///
/// The high watermark (end offset) of each assigned partition is snapshotted before consuming.
/// Records at offsets below a partition's watermark are the live state that must be replayed.
/// <see cref="BlockedUsersStateReady"/> is signalled only once every assigned partition has been
/// replayed, so the main consumer starts processing with an accurate <c>IsBlocked()</c> view.
/// The caller keeps consuming afterwards so the store stays current with new block/unblock
/// updates.
/// </summary>
public sealed class BlockedUsersStateRebuilder(
    IConsumer<string, byte[]> consumer,
    string topic,
    BlockedUsersStateReady ready,
    ILogger logger
)
{
    private Dictionary<TopicPartition, long> _watermarks = [];
    private readonly HashSet<TopicPartition> _replayedPartitions = [];
    private bool _rebuildSignalled;

    /// <summary>
    /// Returns the high watermark (end offset) of every partition this consumer is assigned on
    /// the state topic, i.e. the offset of the next record to be appended on each partition.
    /// Retries until every assigned partition is known to the broker (the topic may not exist yet
    /// at startup).
    /// </summary>
    public Dictionary<TopicPartition, long> SnapshotWatermarks(CancellationToken ct)
    {
        var assignment = consumer.Assignment;

        while (!ct.IsCancellationRequested)
        {
            var watermarks = new Dictionary<TopicPartition, long>();
            var complete = true;

            foreach (var topicPartition in assignment)
            {
                try
                {
                    var offsets = consumer.QueryWatermarkOffsets(topicPartition, TimeSpan.FromSeconds(5));
                    watermarks[topicPartition] = offsets.High.Value;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Could not snapshot watermark for {Topic} partition {Partition}; retrying",
                        topicPartition.Topic,
                        topicPartition.Partition
                    );
                    complete = false;
                    break;
                }
            }

            if (complete)
            {
                _watermarks = watermarks;
                return _watermarks;
            }

            Task.Delay(TimeSpan.FromMilliseconds(250), ct).Wait(ct);
        }

        throw new OperationCanceledException(ct);
    }

    /// <summary>
    /// Records that a partition has been replayed past its startup watermark and signals
    /// readiness once every assigned partition has been replayed. Returns true once the rebuild
    /// has been signalled, so the caller can stop checking.
    /// </summary>
    public bool TrySignalRebuild(TopicPartition topicPartition, long offset)
    {
        if (_rebuildSignalled)
            return true;

        if (!_watermarks.TryGetValue(topicPartition, out var watermark))
            return false;

        if (offset < watermark || !_replayedPartitions.Add(topicPartition))
            return false;

        if (_replayedPartitions.Count != _watermarks.Count)
            return false;

        _rebuildSignalled = true;
        ready.Signal();
        logger.LogInformation(
            "Blocked-users store rebuilt from topic {Topic} ({PartitionCount} partitions)",
            topic,
            _watermarks.Count
        );
        return true;
    }
}
