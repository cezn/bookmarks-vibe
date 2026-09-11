using System.Text;
using Confluent.Kafka;

namespace KafkaConsumerApp.Dlq;

public enum DlqProcessingOutcome
{
    StoreOffset = 1,
    Seek = 2,
}

public interface IKafkaDlqFacade
{
    bool IsReplay(ConsumeResult<string, byte[]> result);
    bool IsBlocked(string? userId);
    Task<DlqProcessingOutcome> HandleFailureAsync(
        ConsumeResult<string, byte[]> item,
        string reason,
        string? type,
        string? userId,
        CancellationToken ct
    );
    Task HandleReplaySuccessAsync(string userId, CancellationToken ct);
}

public sealed class KafkaDlqFacade(
    IDeadLetterPublisher deadLetterPublisher,
    IBlockedUsersStore blockedUsersStore,
    KafkaDlqOptions options
) : IKafkaDlqFacade
{
    public bool IsReplay(ConsumeResult<string, byte[]> result)
    {
        if (!result.Message.Headers.TryGetLastBytes(options.ReplayHeaderName, out var replayHeader))
            return false;

        var replayHeaderValue = Encoding.UTF8.GetString(replayHeader).Trim();
        return replayHeaderValue.Equals(options.ReplayHeaderValue, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsBlocked(string? userId) => !string.IsNullOrWhiteSpace(userId) && blockedUsersStore.IsBlocked(userId);

    public async Task<DlqProcessingOutcome> HandleFailureAsync(
        ConsumeResult<string, byte[]> item,
        string reason,
        string? type,
        string? userId,
        CancellationToken ct
    )
    {
        var deadLettered = await deadLetterPublisher.TryPublishDeadLetter(item, reason, type, ct);
        if (!deadLettered)
            return DlqProcessingOutcome.Seek;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var blocked = await deadLetterPublisher.TryPublishBlockedUserState(
                userId,
                blocked: true,
                reason: reason,
                ct
            );

            if (!blocked)
                return DlqProcessingOutcome.Seek;

            blockedUsersStore.Block(userId);
        }

        return DlqProcessingOutcome.StoreOffset;
    }

    public async Task HandleReplaySuccessAsync(string userId, CancellationToken ct)
    {
        var unblocked = await deadLetterPublisher.TryPublishBlockedUserState(
            userId,
            blocked: false,
            reason: "replay_succeeded",
            ct
        );

        if (unblocked)
            blockedUsersStore.Unblock(userId);
    }
}
