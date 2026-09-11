namespace TagsBookmarksSubscriber;

public record Tag(int Id, string Name, int UsageCount, DateTimeOffset CreatedAt, string UserId);
