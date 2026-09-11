using BookmarksApi.Bookmarks;

namespace BookmarksApi.Tests.Utils;

public record BookmarkBuilder
{
    private int _id = -1;
    private string _title = "New Bookmark " + Guid.CreateVersion7();
    private string _url = "https://example.com";
    private string? _summary = "A summary for post";
    private DateTimeOffset _createdAt = new(2024, 1, 1, 13, 0, 0, TimeSpan.Zero);
    private DateTimeOffset _updatedAt = new(2024, 1, 1, 16, 0, 0, TimeSpan.Zero);
    private List<string> _tags = ["post", "api"];
    private string _userId = "570caa91-8a71-467c-9ff7-1e104d84b2f1";

    public BookmarkBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public BookmarkBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public BookmarkBuilder WithUrl(string url)
    {
        _url = url;
        return this;
    }

    public BookmarkBuilder WithSummary(string? summary)
    {
        _summary = summary;
        return this;
    }

    public BookmarkBuilder CreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public BookmarkBuilder UpdatedAt(DateTimeOffset updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public BookmarkBuilder WithTags(params string[] tags)
    {
        _tags = [.. tags];
        return this;
    }

    public BookmarkBuilder AddTag(string tag)
    {
        _tags.Add(tag);
        return this;
    }

    public BookmarkBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public Bookmark Build() =>
        new(
            Id: _id,
            Title: _title,
            Url: _url,
            Summary: _summary,
            CreatedAt: _createdAt,
            UpdatedAt: _updatedAt,
            Tags: [.. _tags],
            UserId: _userId
        );
}
