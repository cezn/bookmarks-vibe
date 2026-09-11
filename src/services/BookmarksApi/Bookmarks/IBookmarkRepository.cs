using BookmarksApi.Shared;
using Npgsql;

namespace BookmarksApi.Bookmarks;

public interface IBookmarkRepository
{
    NpgsqlConnection Connection { get; }
    Task<Bookmark?> GetBookmarkByIdAsync(int id, string userId);
    Task<IEnumerable<Bookmark>> GetBookmarksByTagAsync(string? tag, string userId);
    Task<IEnumerable<Bookmark>> SearchBookmarksCursorAsync(
        string? q,
        string userId,
        BookmarkSort sort = BookmarkSort.Id,
        SortDirection sortDirection = SortDirection.Asc,
        int limit = 20,
        Cursor? cursor = null,
        string[]? tags = null
    );
    Task<int> AddBookmarkAsync(Bookmark bookmark);
    Task<bool> RemoveBookmarkAsync(int id, string userId);
    Task<bool> UpdateBookmarkAsync(Bookmark updatedBookmark);
    Task UpsertBookmarkAsync(Bookmark bookmark);
    Task<int> RemoveTagFromAllBookmarksAsync(string tag, string userId, DateTimeOffset updatedAt);
    Task<int> RenameTagOnAllBookmarksAsync(string oldName, string newName, string userId);
    Task<bool> ArchiveBookmarkAsync(int id, string userId, DateTimeOffset archivedAt);
    Task<bool> RestoreBookmarkAsync(int id, string userId);
    Task<bool> PermanentlyDeleteBookmarkAsync(int id, string userId);
    Task<IEnumerable<Bookmark>> GetArchivedBookmarksCursorAsync(string userId, int limit = 20, Cursor? cursor = null);
    Task<int> ArchiveBookmarksByIdsAsync(int[] ids, string userId, DateTimeOffset archivedAt);
    Task<int> RestoreBookmarksByIdsAsync(int[] ids, string userId);
}
