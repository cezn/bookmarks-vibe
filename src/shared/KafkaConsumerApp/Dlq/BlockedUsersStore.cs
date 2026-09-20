using System.Collections.Concurrent;

namespace KafkaConsumerApp.Dlq;

public interface IBlockedUsersStore
{
    bool IsBlocked(string userId);

    /// <summary>
    /// Marks the user as blocked, but only if <paramref name="version"/> is newer than the
    /// last applied version for that user. The version is the Kafka offset of the state message,
    /// which is monotonic per key within its partition on the compacted state topic.
    /// </summary>
    void Block(string userId, long version);

    /// <summary>
    /// Marks the user as unblocked, but only if <paramref name="version"/> is newer than the
    /// last applied version for that user.
    /// </summary>
    void Unblock(string userId, long version);
}

public sealed class InMemoryBlockedUsersStore : IBlockedUsersStore
{
    private readonly ConcurrentDictionary<string, (long Version, bool Blocked)> _state = new(StringComparer.Ordinal);

    public bool IsBlocked(string userId) =>
        !string.IsNullOrWhiteSpace(userId) && _state.TryGetValue(userId, out var s) && s.Blocked;

    public void Block(string userId, long version)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        Apply(userId, version, blocked: true);
    }

    public void Unblock(string userId, long version)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        Apply(userId, version, blocked: false);
    }

    private void Apply(string userId, long version, bool blocked)
    {
        while (true)
        {
            if (!_state.TryGetValue(userId, out var current))
            {
                // No prior state: accept the first update we see.
                if (_state.TryAdd(userId, (version, blocked)))
                    return;
                continue; // lost the add race, re-read
            }

            if (version <= current.Version)
                return; // stale or duplicate update, ignore

            if (_state.TryUpdate(userId, (version, blocked), current))
                return;
            // lost the update race, re-read and retry
        }
    }
}
