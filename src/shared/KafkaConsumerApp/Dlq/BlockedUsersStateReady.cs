namespace KafkaConsumerApp.Dlq;

/// <summary>
/// A one-shot readiness gate that signals when <see cref="IBlockedUsersStore"/> has been fully
/// rebuilt from the compacted blocked-users state topic.
///
/// The main consumer awaits <see cref="WaitUntilReadyAsync"/> before it starts processing, so no
/// message is handled (and no user is wrongly treated as unblocked) until the in-memory store
/// reflects the topic's state. The gate is safe to await from any number of consumers and is
/// idempotent: once signalled it stays signalled.
/// </summary>
public sealed class BlockedUsersStateReady
{
    private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Completes the gate, indicating the store has been rebuilt. Safe to call more than once.
    /// </summary>
    public void Signal() => _tcs.TrySetResult();

    /// <summary>
    /// Waits until the store has been rebuilt (or the token is cancelled).
    /// </summary>
    public Task WaitUntilReadyAsync(CancellationToken ct) => _tcs.Task.WaitAsync(ct);
}
