using System.Data.Common;
using BookmarksApi.Shared;
using Npgsql;

namespace BookmarksApi.Bookmarks;

using static BookmarksApi.Bookmarks.BookmarkSort;

public static class BookmarkDbConnectionExtensions
{
    public static async Task<Bookmark?> GetBookmarkByIdAsync(
        this NpgsqlConnection con,
        int id,
        string userId,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "SELECT id, title, url, summary, created_at, update_at, tags, user_id, archived_at FROM bookmarks WHERE id = $1 AND user_id = $2";
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(userId);
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("SelectBookmarkById", ct: ct);
        if (await reader.ReadAsync(ct))
        {
            return new Bookmark(
                Id: reader.GetInt32(0),
                Title: reader.GetString(1),
                Url: reader.GetString(2),
                Summary: reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedAt: reader.GetFieldValue<DateTimeOffset>(4),
                UpdatedAt: reader.GetFieldValue<DateTimeOffset>(5),
                Tags: reader.GetFieldValue<string[]>(6).ToList(),
                UserId: reader.GetString(7),
                ArchivedAt: reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8)
            );
        }
        return null;
    }

    public static async Task<IEnumerable<Bookmark>> GetBookmarksByTagAsync(
        this NpgsqlConnection con,
        string? tag,
        string userId,
        bool forUpdate = false,
        CancellationToken ct = default
    )
    {
        var bookmarks = new List<Bookmark>();
        if (string.IsNullOrWhiteSpace(tag))
            return bookmarks;

        using var cmd = con.CreateCommand();
        cmd.CommandText = $"""
            SELECT id, title, url, summary, created_at, update_at, tags, user_id, archived_at
            FROM bookmarks WHERE $1 = ANY(tags)
            AND user_id = $2
            AND archived_at IS NULL
            {(forUpdate ? "FOR UPDATE" : "")}
            """;
        cmd.Parameters.AddWithValue(tag);
        cmd.Parameters.AddWithValue(userId);

        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("SelectBookmarksByTag", ct: ct);
        while (await reader.ReadAsync(ct))
        {
            bookmarks.Add(
                new Bookmark(
                    Id: reader.GetInt32(0),
                    Title: reader.GetString(1),
                    Url: reader.GetString(2),
                    Summary: reader.IsDBNull(3) ? null : reader.GetString(3),
                    CreatedAt: reader.GetFieldValue<DateTimeOffset>(4),
                    UpdatedAt: reader.GetFieldValue<DateTimeOffset>(5),
                    Tags: reader.GetFieldValue<string[]>(6).ToList(),
                    UserId: reader.GetString(7),
                    ArchivedAt: reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8)
                )
            );
        }
        return bookmarks;
    }

    public static async Task<IEnumerable<Bookmark>> SearchBookmarksCursorAsync(
        this NpgsqlConnection con,
        string? q,
        string userId,
        BookmarkSort sort = Id,
        SortDirection sortDirection = SortDirection.Asc,
        int limit = 20,
        Cursor? cursor = null,
        string[]? tags = null,
        CancellationToken ct = default
    )
    {
        var sortField = sort switch
        {
            CreatedAt => "created_at",
            UpdatedAt => "update_at",
            Id => "id",
            Title => "title",
            _ => "id",
        };
        var order = sortDirection == SortDirection.Desc ? "DESC" : "ASC";
        var sql = "SELECT id, title, url, summary, created_at, update_at, tags, user_id, archived_at FROM bookmarks";
        var whereClauses = new List<string>();
        var parameters = new List<object>();

        whereClauses.Add("user_id = $1");
        parameters.Add(userId);
        whereClauses.Add("archived_at IS NULL");

        if (!string.IsNullOrWhiteSpace(q))
        {
            var paramIndex = parameters.Count + 1;
            whereClauses.Add($"search_vector @@ plainto_tsquery('english', ${paramIndex})");
            parameters.Add(q);
        }

        if (tags is not null && tags.Length > 0)
        {
            var paramIndex = parameters.Count + 1;
            whereClauses.Add($"tags @> ${paramIndex}");
            parameters.Add(tags);
        }

        if (cursor is not null)
        {
            var paramOffset = parameters.Count + 1;
            whereClauses.Add(
                order == "DESC"
                    ? $"( {sortField} < ${paramOffset} OR ({sortField} = ${paramOffset} AND id < ${paramOffset + 1}) )"
                    : $"( {sortField} > ${paramOffset} OR ({sortField} = ${paramOffset} AND id > ${paramOffset + 1}) )"
            );
            parameters.Add(
                sort switch
                {
                    Id => int.Parse(cursor.SortValue),
                    CreatedAt or UpdatedAt => DateTimeOffset.Parse(cursor.SortValue),
                    Title => cursor.SortValue,
                    _ => cursor.SortValue,
                }
            );
            parameters.Add(cursor.Id);
        }

        if (whereClauses.Count > 0)
            sql += " WHERE " + string.Join(" AND ", whereClauses);
        sql += $" ORDER BY {sortField} {order}, id {order} LIMIT ${parameters.Count + 1}";
        parameters.Add(limit);

        using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        for (int i = 0; i < parameters.Count; i++)
            cmd.Parameters.AddWithValue(parameters[i]);

        var bookmarks = new List<Bookmark>();
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("SearchBookmarksCursor", ct: ct);
        while (await reader.ReadAsync(ct))
        {
            bookmarks.Add(
                new Bookmark(
                    Id: reader.GetInt32(0),
                    Title: reader.GetString(1),
                    Url: reader.GetString(2),
                    Summary: reader.IsDBNull(3) ? null : reader.GetString(3),
                    CreatedAt: reader.GetFieldValue<DateTimeOffset>(4),
                    UpdatedAt: reader.GetFieldValue<DateTimeOffset>(5),
                    Tags: reader.GetFieldValue<string[]>(6).ToList(),
                    UserId: reader.GetString(7),
                    ArchivedAt: reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8)
                )
            );
        }

        return bookmarks;
    }

    public static async Task<int> AddBookmarkAsync(
        this NpgsqlConnection con,
        Bookmark bookmark,
        CancellationToken ct = default
    )
    {
        using DbCommand cmd = con.CreateCommand();
        cmd.CommandText =
            "INSERT INTO bookmarks (title, url, summary, created_at, update_at, tags, user_id) VALUES ($1, $2, $3, $4, $5, $6, $7) RETURNING id";
        cmd.Parameters.Add(new NpgsqlParameter { Value = bookmark.Title });
        cmd.Parameters.Add(new NpgsqlParameter { Value = bookmark.Url });
        cmd.Parameters.Add(new NpgsqlParameter { Value = bookmark.Summary ?? (object)DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter { Value = bookmark.CreatedAt });
        cmd.Parameters.Add(new NpgsqlParameter { Value = bookmark.UpdatedAt });
        cmd.Parameters.Add(new NpgsqlParameter { Value = bookmark.Tags.ToArray() });
        cmd.Parameters.Add(new NpgsqlParameter { Value = bookmark.UserId });
        var result = await ((NpgsqlCommand)cmd).ExecuteScalarWithSpanNameAsync("InsertBookmark", ct: ct);
        return Convert.ToInt32(result);
    }

    public static async Task<bool> RemoveBookmarkAsync(
        this NpgsqlConnection con,
        int id,
        string userId,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM bookmarks WHERE id = $1 AND user_id = $2";
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(userId);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("DeleteBookmarkById", ct: ct) > 0;
    }

    public static async Task<bool> UpdateBookmarkAsync(
        this NpgsqlConnection con,
        Bookmark updatedBookmark,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "UPDATE bookmarks SET title = $1, url = $2, summary = $3, created_at = $4, update_at = $5, tags = $6 WHERE id = $7 AND user_id = $8";
        cmd.Parameters.AddWithValue(updatedBookmark.Title);
        cmd.Parameters.AddWithValue(updatedBookmark.Url);
        cmd.Parameters.AddWithValue(updatedBookmark.Summary ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue(updatedBookmark.CreatedAt);
        cmd.Parameters.AddWithValue(updatedBookmark.UpdatedAt);
        cmd.Parameters.AddWithValue(updatedBookmark.Tags.ToArray());
        cmd.Parameters.AddWithValue(updatedBookmark.Id);
        cmd.Parameters.AddWithValue(updatedBookmark.UserId);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("UpdateBookmarkById", ct: ct) > 0;
    }

    public static async Task UpsertBookmarkAsync(
        this NpgsqlConnection con,
        Bookmark bookmark,
        CancellationToken ct = default
    )
    {
        // this won't work as id is generated always and can't be manually inserted.
        // Ids from untrusted clients should not be inserted either.
        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            MERGE INTO bookmarks AS t
            USING (VALUES ($1, $2, $3, $4, $5, $6, $7, $8)) AS s(id, title, url, summary, created_at, update_at, tags, user_id)
            ON t.id = s.id AND t.user_id = s.user_id
            WHEN MATCHED THEN
                UPDATE SET title = s.title, url = s.url, summary = s.summary, created_at = s.created_at, update_at = s.update_at, tags = s.tags
            WHEN NOT MATCHED THEN
                INSERT (id, title, url, summary, created_at, update_at, tags, user_id)
                VALUES (s.id, s.title, s.url, s.summary, s.created_at, s.update_at, s.tags, s.user_id);
            """;
        cmd.Parameters.AddWithValue(bookmark.Id);
        cmd.Parameters.AddWithValue(bookmark.Title);
        cmd.Parameters.AddWithValue(bookmark.Url);
        cmd.Parameters.AddWithValue(bookmark.Summary ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue(bookmark.CreatedAt);
        cmd.Parameters.AddWithValue(bookmark.UpdatedAt);
        cmd.Parameters.AddWithValue(bookmark.Tags.ToArray());
        cmd.Parameters.AddWithValue(bookmark.UserId);
        await cmd.ExecuteNonQueryWithSpanNameAsync("UpsertBookmark", ct: ct);
    }

    public static async Task<int> RemoveTagFromAllBookmarksAsync(
        this NpgsqlConnection con,
        string tag,
        string userId,
        DateTimeOffset updatedAt,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE bookmarks
            SET tags = array_remove(tags, $1),
                update_at = $2
            WHERE $1 = ANY(tags) AND user_id = $3 AND archived_at IS NULL
            """;
        cmd.Parameters.AddWithValue(tag);
        cmd.Parameters.AddWithValue(updatedAt);
        cmd.Parameters.AddWithValue(userId);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("RemoveTagFromAllBookmarks", ct: ct);
    }

    public static async Task<int> RenameTagOnAllBookmarksAsync(
        this NpgsqlConnection con,
        string oldName,
        string newName,
        string userId,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE bookmarks
            SET tags = (
                SELECT array_agg(DISTINCT CASE WHEN tag = $1 THEN $2 ELSE tag END)
                FROM unnest(tags) AS tag
            )
            WHERE $1 = ANY(tags) AND user_id = $3 AND archived_at IS NULL
            """;
        cmd.Parameters.AddWithValue(oldName);
        cmd.Parameters.AddWithValue(newName);
        cmd.Parameters.AddWithValue(userId);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("RenameTagOnAllBookmarks", ct: ct);
    }

    public static async Task<int> MoveTagToFrontOnAllBookmarksAsync(
        this NpgsqlConnection con,
        string tag,
        string userId,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE bookmarks
            SET tags = array_prepend($1, array_remove(tags, $1))
            WHERE $1 = ANY(tags) AND user_id = $2 AND archived_at IS NULL
            """;
        cmd.Parameters.AddWithValue(tag);
        cmd.Parameters.AddWithValue(userId);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("MoveTagToFrontOnAllBookmarks", ct: ct);
    }

    public static async Task<int> MoveTagToEndOnAllBookmarksAsync(
        this NpgsqlConnection con,
        string tag,
        string userId,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE bookmarks
            SET tags = array_append(array_remove(tags, $1), $1)
            WHERE $1 = ANY(tags) AND user_id = $2 AND archived_at IS NULL
            """;
        cmd.Parameters.AddWithValue(tag);
        cmd.Parameters.AddWithValue(userId);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("MoveTagToEndOnAllBookmarks", ct: ct);
    }

    public static async Task<bool> ArchiveBookmarkAsync(
        this NpgsqlConnection con,
        int id,
        string userId,
        DateTimeOffset archivedAt,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "UPDATE bookmarks SET archived_at = $1 WHERE id = $2 AND user_id = $3 AND archived_at IS NULL";
        cmd.Parameters.AddWithValue(archivedAt);
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(userId);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("ArchiveBookmark", ct: ct) > 0;
    }

    public static async Task<bool> RestoreBookmarkAsync(
        this NpgsqlConnection con,
        int id,
        string userId,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "UPDATE bookmarks SET archived_at = NULL WHERE id = $1 AND user_id = $2 AND archived_at IS NOT NULL";
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(userId);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("RestoreBookmark", ct: ct) > 0;
    }

    public static async Task<bool> PermanentlyDeleteBookmarkAsync(
        this NpgsqlConnection con,
        int id,
        string userId,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM bookmarks WHERE id = $1 AND user_id = $2 AND archived_at IS NOT NULL";
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(userId);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("PermanentlyDeleteBookmark", ct: ct) > 0;
    }

    public static async Task<IEnumerable<Bookmark>> GetArchivedBookmarksCursorAsync(
        this NpgsqlConnection con,
        string userId,
        int limit = 20,
        Cursor? cursor = null,
        CancellationToken ct = default
    )
    {
        var sql = "SELECT id, title, url, summary, created_at, update_at, tags, user_id, archived_at FROM bookmarks";
        var whereClauses = new List<string>();
        var parameters = new List<object>();

        whereClauses.Add("user_id = $1");
        parameters.Add(userId);
        whereClauses.Add("archived_at IS NOT NULL");

        if (cursor is not null)
        {
            var paramOffset = parameters.Count + 1;
            whereClauses.Add(
                $"( archived_at < ${paramOffset} OR (archived_at = ${paramOffset} AND id < ${paramOffset + 1}) )"
            );
            parameters.Add(DateTimeOffset.Parse(cursor.SortValue));
            parameters.Add(cursor.Id);
        }

        if (whereClauses.Count > 0)
            sql += " WHERE " + string.Join(" AND ", whereClauses);
        sql += $" ORDER BY archived_at DESC, id DESC LIMIT ${parameters.Count + 1}";
        parameters.Add(limit);

        using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        for (int i = 0; i < parameters.Count; i++)
            cmd.Parameters.AddWithValue(parameters[i]);

        var bookmarks = new List<Bookmark>();
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("GetArchivedBookmarksCursor", ct: ct);
        while (await reader.ReadAsync(ct))
        {
            bookmarks.Add(
                new Bookmark(
                    Id: reader.GetInt32(0),
                    Title: reader.GetString(1),
                    Url: reader.GetString(2),
                    Summary: reader.IsDBNull(3) ? null : reader.GetString(3),
                    CreatedAt: reader.GetFieldValue<DateTimeOffset>(4),
                    UpdatedAt: reader.GetFieldValue<DateTimeOffset>(5),
                    Tags: reader.GetFieldValue<string[]>(6).ToList(),
                    UserId: reader.GetString(7),
                    ArchivedAt: reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8)
                )
            );
        }

        return bookmarks;
    }

    public static async Task<int> ArchiveBookmarksByIdsAsync(
        this NpgsqlConnection con,
        int[] ids,
        string userId,
        DateTimeOffset archivedAt,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "UPDATE bookmarks SET archived_at = $1 WHERE user_id = $2 AND id = ANY($3) AND archived_at IS NULL";
        cmd.Parameters.AddWithValue(archivedAt);
        cmd.Parameters.AddWithValue(userId);
        cmd.Parameters.AddWithValue(ids);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("ArchiveBookmarksByIds", ct: ct);
    }

    public static async Task<int> RestoreBookmarksByIdsAsync(
        this NpgsqlConnection con,
        int[] ids,
        string userId,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "UPDATE bookmarks SET archived_at = NULL WHERE user_id = $1 AND id = ANY($2) AND archived_at IS NOT NULL";
        cmd.Parameters.AddWithValue(userId);
        cmd.Parameters.AddWithValue(ids);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("RestoreBookmarksByIds", ct: ct);
    }

    public static async Task<IEnumerable<(string Tag, int Count)>> GetTagsForSearchAsync(
        this NpgsqlConnection con,
        string? q,
        string userId,
        string[]? tags = null,
        CancellationToken ct = default
    )
    {
        var tagList = new List<(string, int)>();
        var sql =
            "SELECT unnest(tags) as tag, COUNT(*) as count FROM bookmarks WHERE user_id = $1 AND archived_at IS NULL";
        var parameters = new List<object> { userId };

        if (!string.IsNullOrWhiteSpace(q))
        {
            var paramIndex = parameters.Count + 1;
            sql += $" AND search_vector @@ plainto_tsquery('english', ${paramIndex})";
            parameters.Add(q);
        }

        if (tags is not null && tags.Length > 0)
        {
            var paramIndex = parameters.Count + 1;
            sql += $" AND tags @> ${paramIndex}";
            parameters.Add(tags);
        }

        sql += " GROUP BY tag ORDER BY COUNT(*) DESC";

        using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        for (int i = 0; i < parameters.Count; i++)
            cmd.Parameters.AddWithValue(parameters[i]);

        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("GetTagsForSearch", ct: ct);
        while (await reader.ReadAsync(ct))
        {
            tagList.Add((reader.GetString(0), reader.GetInt32(1)));
        }

        return tagList;
    }
}
