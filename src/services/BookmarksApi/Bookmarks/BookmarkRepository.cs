using BookmarksApi.Shared;
using Npgsql;

namespace BookmarksApi.Bookmarks;

public class BookmarkRepository(NpgsqlConnection connection) : IBookmarkRepository
{
    public NpgsqlConnection Connection => connection;

    public Task<Bookmark?> GetBookmarkByIdAsync(int id, string userId) => connection.GetBookmarkByIdAsync(id, userId);

    public Task<IEnumerable<Bookmark>> GetBookmarksByTagAsync(string? tag, string userId) =>
        connection.GetBookmarksByTagAsync(tag, userId);

    public Task<IEnumerable<Bookmark>> SearchBookmarksCursorAsync(
        string? q,
        string userId,
        BookmarkSort sort = BookmarkSort.Id,
        SortDirection sortDirection = SortDirection.Asc,
        int limit = 20,
        Cursor? cursor = null,
        string[]? tags = null
    ) => connection.SearchBookmarksCursorAsync(q, userId, sort, sortDirection, limit, cursor, tags);

    public Task<int> AddBookmarkAsync(Bookmark bookmark) => connection.AddBookmarkAsync(bookmark);

    public Task<bool> RemoveBookmarkAsync(int id, string userId) => connection.RemoveBookmarkAsync(id, userId);

    public Task<bool> UpdateBookmarkAsync(Bookmark updatedBookmark) => connection.UpdateBookmarkAsync(updatedBookmark);

    public Task UpsertBookmarkAsync(Bookmark bookmark) => connection.UpsertBookmarkAsync(bookmark);

    public Task<int> RemoveTagFromAllBookmarksAsync(string tag, string userId, DateTimeOffset updatedAt) =>
        connection.RemoveTagFromAllBookmarksAsync(tag, userId, updatedAt);

    public Task<int> RenameTagOnAllBookmarksAsync(string oldName, string newName, string userId) =>
        connection.RenameTagOnAllBookmarksAsync(oldName, newName, userId);

    public Task<bool> ArchiveBookmarkAsync(int id, string userId, DateTimeOffset archivedAt) =>
        connection.ArchiveBookmarkAsync(id, userId, archivedAt);

    public Task<bool> RestoreBookmarkAsync(int id, string userId) => connection.RestoreBookmarkAsync(id, userId);

    public Task<bool> PermanentlyDeleteBookmarkAsync(int id, string userId) =>
        connection.PermanentlyDeleteBookmarkAsync(id, userId);

    public Task<IEnumerable<Bookmark>> GetArchivedBookmarksCursorAsync(
        string userId,
        int limit = 20,
        Cursor? cursor = null
    ) => connection.GetArchivedBookmarksCursorAsync(userId, limit, cursor);

    public Task<int> ArchiveBookmarksByIdsAsync(int[] ids, string userId, DateTimeOffset archivedAt) =>
        connection.ArchiveBookmarksByIdsAsync(ids, userId, archivedAt);

    public Task<int> RestoreBookmarksByIdsAsync(int[] ids, string userId) =>
        connection.RestoreBookmarksByIdsAsync(ids, userId);
}
