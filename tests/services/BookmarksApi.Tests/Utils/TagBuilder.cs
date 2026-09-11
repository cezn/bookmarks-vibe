namespace BookmarksApi.Tests.Utils;

using BookmarksApi.Tags;

public record TagBuilder
{
    private int _id = -1;
    private string _name = "Tag_" + Guid.CreateVersion7().ToString("N");
    private int _usageCount = 1;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private string _userId = "570caa91-8a71-467c-9ff7-1e104d84b2f1";

    public TagBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public TagBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TagBuilder WithUsageCount(int usage)
    {
        _usageCount = usage;
        return this;
    }

    public TagBuilder WithCreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public TagBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public Tag Build() => new(_id, _name, _usageCount, _createdAt, _userId);
}
