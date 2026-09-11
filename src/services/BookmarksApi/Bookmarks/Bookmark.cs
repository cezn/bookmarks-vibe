using System.ComponentModel.DataAnnotations;

namespace BookmarksApi.Bookmarks;

public record Bookmark(
    int Id,
    [Required] string Title,
    [Required] string Url,
    string? Summary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<string> Tags,
    string UserId,
    DateTimeOffset? ArchivedAt = null
);
