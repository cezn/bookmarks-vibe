using System.Collections.Concurrent;

namespace KafkaConsumerApp.Dlq;

public interface IBlockedUsersStore
{
    bool IsBlocked(string userId);
    void Block(string userId);
    void Unblock(string userId);
}

public sealed class InMemoryBlockedUsersStore : IBlockedUsersStore
{
    private readonly ConcurrentDictionary<string, byte> _blockedUsers = new(StringComparer.Ordinal);

    public bool IsBlocked(string userId) => _blockedUsers.ContainsKey(userId);

    public void Block(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        _blockedUsers[userId] = 1;
    }

    public void Unblock(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        _blockedUsers.TryRemove(userId, out _);
    }
}
