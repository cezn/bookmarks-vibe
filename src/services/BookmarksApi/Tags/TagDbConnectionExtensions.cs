using BookmarksApi.Shared;
using Npgsql;

namespace BookmarksApi.Tags;

using static BookmarksApi.Tags.TagSort;

public static class TagDbConnectionExtensions
{
    public static async Task<IEnumerable<Tag>> GetAllTagsAsync(
        this NpgsqlConnection con,
        string userId,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, usage_count, created_at, user_id
            FROM tags
            WHERE user_id = $1
            ORDER BY id
            """;
        cmd.Parameters.AddWithValue(userId ?? (object)DBNull.Value);
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("GetAllTags", ct);
        var tags = new List<Tag>();
        while (await reader.ReadAsync(ct))
            tags.Add(
                new Tag(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetDateTime(3),
                    reader.GetString(4)
                )
            );

        return tags;
    }

    public static async Task<IEnumerable<Tag>> GetTagsByNamesAsync(
        this NpgsqlConnection con,
        string[] names,
        string userId
    )
    {
        if (names.Length == 0)
            return [];

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, usage_count, created_at, user_id
            FROM tags
            WHERE name = ANY($1) AND user_id = $2
            ORDER BY id
            """;
        cmd.Parameters.AddWithValue(names);
        cmd.Parameters.AddWithValue(userId);
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("GetTagsByNames");
        var tags = new List<Tag>();
        while (await reader.ReadAsync())
            tags.Add(
                new Tag(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetDateTime(3),
                    reader.GetString(4)
                )
            );

        return tags;
    }

    public static async Task<IEnumerable<Tag>> SearchTagsCursorAsync(
        this NpgsqlConnection con,
        string userId,
        TagSort sort = UsageCount,
        SortDirection sortDirection = SortDirection.Asc,
        Cursor? cursor = null,
        int limit = 20,
        string? q = null,
        CancellationToken ct = default
    )
    {
        var sortField = sort switch
        {
            Name => "name",
            Id => "id",
            UsageCount => "usage_count",
            CreatedAt => "created_at",
            _ => "id",
        };
        var order = sortDirection == SortDirection.Desc ? "DESC" : "ASC";
        var sql = "SELECT id, name, usage_count, created_at, user_id FROM tags";
        var whereClauses = new List<string>();
        var parameters = new List<object>();

        whereClauses.Add("user_id = $1");
        parameters.Add(userId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            whereClauses.Add("search_vector @@ plainto_tsquery('english', $2)");
            parameters.Add(q);
        }

        if (cursor is not null)
        {
            var paramOffset = parameters.Count + 1;
            var clause =
                order == "ASC"
                    ? $"({sortField} > ${paramOffset} OR ({sortField} = ${paramOffset} AND id > ${paramOffset + 1}))"
                    : $"({sortField} < ${paramOffset} OR ({sortField} = ${paramOffset} AND id < ${paramOffset + 1}))";

            whereClauses.Add(clause);
            parameters.Add(
                sort switch
                {
                    Name => cursor.SortValue,
                    Id => int.Parse(cursor.SortValue),
                    UsageCount => int.Parse(cursor.SortValue),
                    CreatedAt => DateTimeOffset.Parse(cursor.SortValue),
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

        var tags = new List<Tag>();
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("GetTagsCursor", ct: ct);
        while (await reader.ReadAsync(ct))
            tags.Add(
                new Tag(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetDateTime(3),
                    reader.GetString(4)
                )
            );

        return tags;
    }

    public static async Task<Tag> AddTagAsync(this NpgsqlConnection con, Tag tag, CancellationToken ct = default)
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            INSERT INTO tags (name, usage_count, created_at, user_id)
            VALUES ($1, 1, $2, $3)
            ON CONFLICT (name, user_id) DO UPDATE
            SET usage_count = tags.usage_count + 1
            RETURNING id, name, usage_count, created_at, user_id
            """;
        cmd.Parameters.AddWithValue(tag.Name);
        cmd.Parameters.AddWithValue(tag.CreatedAt);
        cmd.Parameters.AddWithValue(tag.UserId);
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("AddTag", ct: ct);

        if (await reader.ReadAsync(ct))
            return new Tag(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetDateTime(3),
                reader.GetString(4)
            );

        // This should not happen due to the RETURNING clause
        throw new InvalidOperationException("Failed to insert or update tag");
    }

    public static async Task<Tag[]> AddTagsAsync(
        this NpgsqlConnection con,
        Tag[] tags,
        string userId,
        CancellationToken ct = default
    )
    {
        if (tags.Length == 0)
            return [];

        var sql = """
            WITH input_tags AS (
                SELECT name, COUNT(*) as count
                FROM unnest($1) as x(name)
                GROUP BY name
            )
            INSERT INTO tags (name, usage_count, created_at, user_id)
            SELECT name, count, now(), $2
            FROM input_tags
            ON CONFLICT (name, user_id) DO UPDATE
            SET usage_count = tags.usage_count + EXCLUDED.usage_count
            RETURNING id, name, usage_count, created_at, user_id
            """;

        using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue(tags.Select(t => t.Name).ToArray());
        cmd.Parameters.AddWithValue(userId);

        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("AddTags", ct: ct);
        var addedTags = new List<Tag>(tags.Length);
        while (await reader.ReadAsync(ct))
            addedTags.Add(
                new Tag(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetDateTime(3),
                    reader.GetString(4)
                )
            );

        return [.. addedTags];
    }

    public static async Task<bool> RemoveTagsByNameAsync(
        this NpgsqlConnection con,
        string[] names,
        string userId,
        CancellationToken ct = default
    )
    {
        if (names.Length == 0)
            return false;

        int affectedRows = 0;

        // First decrement the usage count
        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = """
                UPDATE tags
                SET usage_count = usage_count - 1
                WHERE name = ANY($1) AND usage_count > 0 AND user_id = $2
                """;
            cmd.Parameters.AddWithValue(names);
            cmd.Parameters.AddWithValue(userId);
            affectedRows = await cmd.ExecuteNonQueryWithSpanNameAsync("RemoveTagsByName_DecrementUsage", ct: ct);
        }

        // Then delete tags with usage_count = 0
        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = """
                DELETE FROM tags
                WHERE name = ANY($1) AND usage_count <= 0 AND user_id = $2
                """;
            cmd.Parameters.AddWithValue(names);
            cmd.Parameters.AddWithValue(userId);
            await cmd.ExecuteNonQueryWithSpanNameAsync("RemoveTagsByName_DeleteIfZero", ct: ct);
        }

        return affectedRows > 0;
    }
}
