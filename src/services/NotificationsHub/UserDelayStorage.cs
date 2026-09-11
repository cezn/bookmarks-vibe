public sealed class UserDelayStorage
{
    private readonly Dictionary<string, int> _userDelays = new();
    private readonly object _lock = new();

    public void SetDelay(string userId, int delay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, 1);

        lock (_lock)
        {
            _userDelays[userId] = delay;
        }
    }

    public int GetDelay(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        lock (_lock)
        {
            return _userDelays.TryGetValue(userId, out var delay) ? delay : 5; // default 5 seconds
        }
    }

    public IReadOnlyDictionary<string, int> GetAllUserDelays()
    {
        lock (_lock)
        {
            return _userDelays.AsReadOnly();
        }
    }
}
