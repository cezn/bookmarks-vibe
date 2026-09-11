namespace BookmarksApi.Outbox;

public record OutboxMessage(
    Guid Id,
    string AggregateType,
    string AggregateId,
    string UserId,
    string Type,
    byte[] Payload,
    DateTime? CreatedAt = null
);
