using BookmarksApi.Shared;

namespace BookmarksApi.Tags;

public record Tag(int Id, string Name, int UsageCount, DateTimeOffset CreatedAt, string UserId)
{
    private DateTimeOffset _createdAt = CreatedAt.TruncateToSeconds();
    public DateTimeOffset CreatedAt
    {
        get { return _createdAt; }
        set { _createdAt = value.TruncateToSeconds(); }
    }
}
