using System.Data;
using System.Text.Json;
using BookmarksApi.Outbox;
using BookmarksApi.Shared;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace BookmarksApi.Bookmarks;

public enum BookmarkSort
{
    Id,
    Title,
    CreatedAt,
    UpdatedAt,
}

public static class BookmarksWebApplicationExtensions
{
    public static void MapBookmarkEndpoints(this WebApplication app)
    {
        app.MapGet("/api/bookmarks", BookmarksEndpoints.GetBookmarks);
        app.MapGet("/api/bookmarks/{id:int}", BookmarksEndpoints.GetBookmarkById);
        app.MapGet("/api/bookmarks/tags", BookmarksEndpoints.GetBookmarksTags);
        app.MapGet("/api/bookmarks/archived", BookmarksEndpoints.GetArchivedBookmarks);
        app.MapPost("/api/bookmarks", BookmarksEndpoints.CreateBookmark);
        app.MapPost("/api/bookmarks/import/msedge", BookmarksEndpoints.ImportMsEdgeBookmarks).DisableAntiforgery();
        app.MapPost("/api/bookmarks/{id:int}/archive", BookmarksEndpoints.ArchiveBookmark);
        app.MapPost("/api/bookmarks/{id:int}/restore", BookmarksEndpoints.RestoreBookmark);
        app.MapDelete("/api/bookmarks/{id:int}", BookmarksEndpoints.DeleteBookmark);
        app.MapDelete("/api/bookmarks/{id:int}/permanent", BookmarksEndpoints.PermanentlyDeleteBookmark);
        app.MapPost("/api/bookmarks/archive", BookmarksEndpoints.ArchiveBookmarks);
        app.MapPost("/api/bookmarks/restore", BookmarksEndpoints.RestoreBookmarks);
        app.MapPut("/api/bookmarks/{id:int}", BookmarksEndpoints.UpdateBookmark);
        app.MapDelete("/api/bookmarks/tags/{tag}", BookmarksEndpoints.RemoveTagFromAllBookmarks);
        app.MapPut("/api/bookmarks/tags/rename", BookmarksEndpoints.RenameTagOnAllBookmarks);
        app.MapPut("/api/bookmarks/tags/move", BookmarksEndpoints.MoveTag);
    }
}

public static class BookmarksEndpoints
{
    public sealed record TagFacet(string Name, int Count);

    public sealed record GetBookmarksResponse(
        IReadOnlyList<Bookmark> Bookmarks,
        IReadOnlyList<TagFacet> Tags,
        string? NextCursor
    );

    /// <summary>
    /// Get user's bookmarks with optional search query, sorting, and pagination
    /// </summary>
    /// <param name="q">
    /// Optional search query string. Supports full-text search with phrases, boolean operators (AND, OR, NOT), and word variations.
    /// E.g. postgres &amp; (linux | windows)
    /// </param>
    /// <param name="tags">Optional array of tag names to filter bookmarks. Bookmarks containing any of these tags will be returned.</param>
    /// <param name="sort">Optional sort field. Values: id, title, createdAt, updatedAt. Default: id</param>
    /// <param name="sortDirection">Optional sort direction. Values: asc, desc. Default: desc</param>
    /// <param name="limit">Maximum number of bookmarks to return per page. Default: 20</param>
    /// <param name="cursor">Optional cursor for pagination. Use the NextCursor from a previous response to get the next page of results. Encoded as a base64 string.</param>
    /// <response code="200">List of bookmarks</response>
    [ProducesResponseType(typeof(GetBookmarksResponse), StatusCodes.Status200OK)]
    internal static async Task<IResult> GetBookmarks(
        UserId userId,
        [FromKeyedServices("ro")] NpgsqlDataSource ds,
        string? q,
        string[]? tags,
        CaseInsensitive<BookmarkSort>? sort,
        int limit = 20,
        CaseInsensitive<SortDirection>? sortDirection = null,
        Cursor? cursor = null,
        CancellationToken ct = default
    )
    {
        sort ??= new(BookmarkSort.Id);
        sortDirection ??= new(SortDirection.Desc);
        limit = limit == 0 ? 20 : limit;

        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        var bookmarks = await FetchBookmarks(userId, q, tags, sort, limit, sortDirection, cursor, con, ct);
        var tagFacets = await FetchTagFacets(userId, q, tags, con, ct);
        var nextCursor = CreateCursorFromBookmarks(sort, limit, bookmarks);

        return Results.Ok(new GetBookmarksResponse(bookmarks, tagFacets, nextCursor?.ToBase64()));

        async Task<List<Bookmark>> FetchBookmarks(
            UserId userId,
            string? q,
            string[]? tags,
            CaseInsensitive<BookmarkSort> sort,
            int limit,
            CaseInsensitive<SortDirection> sortDirection,
            Cursor? cursor,
            NpgsqlConnection con,
            CancellationToken ct
        ) =>
            (
                await con.SearchBookmarksCursorAsync(
                    q: q,
                    userId: userId,
                    sort: sort.Value,
                    sortDirection: sortDirection.Value,
                    cursor: cursor,
                    limit: limit,
                    tags: tags,
                    ct: ct
                )
            ).ToList();

        async Task<List<TagFacet>> FetchTagFacets(
            UserId userId,
            string? q,
            string[]? tags,
            NpgsqlConnection con,
            CancellationToken ct
        ) =>
            (await con.GetTagsForSearchAsync(q: q, userId: userId, tags: tags, ct: ct))
                .Select(t => new TagFacet(t.Tag, t.Count))
                .ToList();

        static Cursor? CreateCursorFromBookmarks(
            CaseInsensitive<BookmarkSort> sort,
            int limit,
            List<Bookmark> bookmarks
        ) =>
            bookmarks.Count == limit && bookmarks.Last() is Bookmark last
                ? new Cursor(
                    SortValue: sort.Value switch
                    {
                        BookmarkSort.Title => last.Title,
                        BookmarkSort.CreatedAt => last.CreatedAt.ToString("o"),
                        BookmarkSort.UpdatedAt => last.UpdatedAt.ToString("o"),
                        BookmarkSort.Id or _ => last.Id.ToString(),
                    },
                    Id: last.Id
                )
                : null;
    }

    /// <summary>
    /// Get a bookmark by ID
    /// </summary>
    /// <param name="id">The bookmark ID</param>
    /// <response code="200">The bookmark</response>
    /// <response code="404">Bookmark not found</response>
    [ProducesResponseType(typeof(Bookmark), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    internal static async Task<IResult> GetBookmarkById(
        int id,
        UserId userId,
        [FromKeyedServices("ro")] NpgsqlDataSource ds,
        CancellationToken ct = default
    )
    {
        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        var bookmark = await con.GetBookmarkByIdAsync(id, userId, ct: ct);

        return bookmark is not null ? Results.Ok(bookmark) : Results.NotFound();
    }

    /// <summary>
    /// Get tags and their counts from user's bookmarks
    /// </summary>
    /// <response code="200">List of tags with counts</response>
    [ProducesResponseType(typeof(IReadOnlyList<TagFacet>), StatusCodes.Status200OK)]
    internal static async Task<IResult> GetBookmarksTags(
        UserId userId,
        [FromKeyedServices("ro")] NpgsqlDataSource ds,
        CancellationToken ct = default
    )
    {
        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        var tags = (await con.GetTagsForSearchAsync(q: null, userId, ct: ct))
            .Select(t => new TagFacet(t.Tag, t.Count))
            .ToList();

        return Results.Ok(tags);
    }

    /// <summary>
    /// Get user's archived bookmarks with cursor-based pagination
    /// </summary>
    /// <param name="limit">Maximum number of bookmarks to return per page. Default: 20</param>
    /// <param name="cursor">Optional cursor for pagination. Use the NextCursor from a previous response to get the next page of results. Encoded as a base64 string.</param>
    /// <response code="200">List of archived bookmarks sorted by archive date descending</response>
    [ProducesResponseType(typeof(GetBookmarksResponse), StatusCodes.Status200OK)]
    internal static async Task<IResult> GetArchivedBookmarks(
        UserId userId,
        [FromKeyedServices("ro")] NpgsqlDataSource ds,
        int limit = 20,
        Cursor? cursor = null,
        CancellationToken ct = default
    )
    {
        limit = limit == 0 ? 20 : limit;

        using var con = await ds.OpenConnectionTraceAsync(ct: ct);

        var bookmarks = (
            await con.GetArchivedBookmarksCursorAsync(userId: userId, limit: limit, cursor: cursor, ct: ct)
        ).ToList();

        Cursor? nextCursor =
            bookmarks.Count == limit && bookmarks.Last() is Bookmark last
                ? new Cursor(SortValue: last.ArchivedAt?.ToString("o") ?? "", Id: last.Id)
                : null;

        return Results.Ok(new GetBookmarksResponse(bookmarks, [], nextCursor?.ToBase64()));
    }

    /// <summary>
    /// Create a new bookmark
    /// </summary>
    /// <param name="bookmark">The bookmark to create</param>
    /// <response code="201">The created bookmark</response>
    [ProducesResponseType(typeof(Bookmark), StatusCodes.Status201Created)]
    internal static async Task<IResult> CreateBookmark(
        Bookmark bookmark,
        UserId userId,
        [FromKeyedServices("rw")] NpgsqlDataSource ds,
        BookmarkOutboxMessageMapper outboxMessageMapper,
        CancellationToken ct = default
    )
    {
        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        using var tran = await con.BeginTransactionAsync(ct);
        var newBookmark = new Bookmark(
            Id: -1,
            Title: bookmark.Title,
            Url: bookmark.Url,
            Summary: bookmark.Summary,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            Tags: bookmark.Tags ?? [],
            UserId: userId
        );

        var id = await con.AddBookmarkAsync(newBookmark, ct: ct);
        newBookmark = newBookmark with { Id = id };
        await con.AddOutboxMessageAsync(await outboxMessageMapper.CreateBookmarkCreatedMessageAsync(newBookmark));
        await tran.CommitAsync(ct);

        return Results.Created($"/api/bookmarks/{newBookmark.Id}", newBookmark);
    }

    /// <summary>
    /// Delete a bookmark by ID
    /// </summary>
    /// <param name="id">The bookmark ID</param>
    /// <response code="204">Bookmark deleted</response>
    /// <response code="404">Bookmark not found</response>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    internal static async Task<IResult> DeleteBookmark(
        int id,
        UserId userId,
        [FromKeyedServices("rw")] NpgsqlDataSource ds,
        BookmarkOutboxMessageMapper outboxMessageMapper,
        CancellationToken ct = default
    )
    {
        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        var bookmark = await con.GetBookmarkByIdAsync(id, userId, ct: ct);
        if (bookmark is null)
            return Results.NotFound();

        using var tran = await con.BeginTransactionAsync(ct);
        await con.RemoveBookmarkAsync(bookmark.Id, userId, ct: ct);
        await con.AddOutboxMessageAsync(await outboxMessageMapper.CreateBookmarkDeletedMessageAsync(bookmark));
        await tran.CommitAsync(ct);

        return Results.NoContent();
    }

    /// <summary>
    /// Archive a bookmark by ID
    /// </summary>
    /// <param name="id">The bookmark ID to archive</param>
    /// <response code="200">The archived bookmark</response>
    /// <response code="404">Bookmark not found or already archived</response>
    [ProducesResponseType(typeof(Bookmark), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    internal static async Task<IResult> ArchiveBookmark(
        int id,
        UserId userId,
        [FromKeyedServices("rw")] NpgsqlDataSource ds,
        BookmarkOutboxMessageMapper outboxMessageMapper,
        CancellationToken ct = default
    )
    {
        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        var bookmark = await con.GetBookmarkByIdAsync(id, userId, ct: ct);
        if (bookmark is null || bookmark.ArchivedAt is not null)
            return Results.NotFound();

        using var tran = await con.BeginTransactionAsync(ct);
        var archivedAt = DateTimeOffset.UtcNow;
        await con.ArchiveBookmarkAsync(id, userId, archivedAt, ct: ct);
        var archivedBookmark = bookmark with { ArchivedAt = archivedAt };
        await con.AddOutboxMessageAsync(await outboxMessageMapper.CreateBookmarkArchivedMessageAsync(archivedBookmark));
        await tran.CommitAsync(ct);

        return Results.Ok(archivedBookmark);
    }

    /// <summary>
    /// Archive multiple bookmarks by IDs
    /// </summary>
    /// <param name="ids">Array of bookmark IDs to archive</param>
    /// <response code="200">Number of bookmarks archived</response>
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    internal static async Task<IResult> ArchiveBookmarks(
        [FromBody] int[] ids,
        UserId userId,
        [FromKeyedServices("rw")] NpgsqlDataSource ds,
        BookmarkOutboxMessageMapper outboxMessageMapper,
        CancellationToken ct = default
    )
    {
        if (ids == null || ids.Length == 0)
            return Results.BadRequest("No bookmark IDs provided");

        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        using var tran = await con.BeginTransactionAsync(ct);
        var archivedAt = DateTimeOffset.UtcNow;
        var count = await con.ArchiveBookmarksByIdsAsync(ids, userId, archivedAt, ct: ct);
        await PublishArchivedBookmarkEvents(ids, userId, archivedAt, outboxMessageMapper, con, ct);
        await tran.CommitAsync(ct);

        return Results.Ok(count);

        static async Task PublishArchivedBookmarkEvents(
            int[] ids,
            UserId userId,
            DateTimeOffset archivedAt,
            BookmarkOutboxMessageMapper outboxMessageMapper,
            NpgsqlConnection con,
            CancellationToken ct
        )
        {
            var bookmarks = new List<Bookmark>();
            foreach (var id in ids)
            {
                var bookmark = await con.GetBookmarkByIdAsync(id, userId, ct: ct);
                if (bookmark is not null)
                    bookmarks.Add(bookmark with { ArchivedAt = archivedAt });
            }

            foreach (var archivedBookmark in bookmarks)
                await con.AddOutboxMessageAsync(
                    await outboxMessageMapper.CreateBookmarkArchivedMessageAsync(archivedBookmark)
                );
        }
    }

    /// <summary>
    /// Restore a bookmark from archive by ID
    /// </summary>
    /// <param name="id">The bookmark ID to restore</param>
    /// <response code="200">The restored bookmark</response>
    /// <response code="404">Bookmark not found or not archived</response>
    [ProducesResponseType(typeof(Bookmark), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    internal static async Task<IResult> RestoreBookmark(
        int id,
        UserId userId,
        [FromKeyedServices("rw")] NpgsqlDataSource ds,
        BookmarkOutboxMessageMapper outboxMessageMapper,
        CancellationToken ct = default
    )
    {
        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        var bookmark = await con.GetBookmarkByIdAsync(id, userId, ct: ct);
        if (bookmark is null || bookmark.ArchivedAt is null)
            return Results.NotFound();

        using var tran = await con.BeginTransactionAsync(ct);
        await con.RestoreBookmarkAsync(id, userId, ct: ct);
        var restoredBookmark = bookmark with { ArchivedAt = null };
        await con.AddOutboxMessageAsync(await outboxMessageMapper.CreateBookmarkRestoredMessageAsync(restoredBookmark));
        await tran.CommitAsync(ct);

        return Results.Ok(restoredBookmark);
    }

    /// <summary>
    /// Restore multiple bookmarks from archive by IDs
    /// </summary>
    /// <param name="ids">Array of bookmark IDs to restore</param>
    /// <response code="200">Number of bookmarks restored</response>
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    internal static async Task<IResult> RestoreBookmarks(
        [FromBody] int[] ids,
        UserId userId,
        [FromKeyedServices("rw")] NpgsqlDataSource ds,
        BookmarkOutboxMessageMapper outboxMessageMapper,
        CancellationToken ct = default
    )
    {
        if (ids == null || ids.Length == 0)
            return Results.BadRequest("No bookmark IDs provided");

        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        using var tran = await con.BeginTransactionAsync(ct);
        var count = await con.RestoreBookmarksByIdsAsync(ids, userId, ct: ct);
        await PublishRestorationEvents(ids, userId, outboxMessageMapper, con, ct);
        await tran.CommitAsync(ct);

        return Results.Ok(count);

        static async Task PublishRestorationEvents(
            int[] ids,
            UserId userId,
            BookmarkOutboxMessageMapper outboxMessageMapper,
            NpgsqlConnection con,
            CancellationToken ct
        )
        {
            var bookmarks = new List<Bookmark>();
            foreach (var id in ids)
            {
                var bookmark = await con.GetBookmarkByIdAsync(id, userId, ct: ct);
                if (bookmark is not null)
                    bookmarks.Add(bookmark with { ArchivedAt = null });
            }

            foreach (var restoredBookmark in bookmarks)
                await con.AddOutboxMessageAsync(
                    await outboxMessageMapper.CreateBookmarkRestoredMessageAsync(restoredBookmark)
                );
        }
    }

    /// <summary>
    /// Permanently delete an archived bookmark by ID
    /// </summary>
    /// <param name="id">The bookmark ID to permanently delete</param>
    /// <response code="204">Bookmark permanently deleted</response>
    /// <response code="404">Bookmark not found or not archived</response>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    internal static async Task<IResult> PermanentlyDeleteBookmark(
        int id,
        UserId userId,
        [FromKeyedServices("rw")] NpgsqlDataSource ds,
        BookmarkOutboxMessageMapper outboxMessageMapper,
        CancellationToken ct = default
    )
    {
        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        var bookmark = await con.GetBookmarkByIdAsync(id, userId, ct: ct);
        if (bookmark is null || bookmark.ArchivedAt is null)
            return Results.NotFound();

        using var tran = await con.BeginTransactionAsync(ct);
        await con.PermanentlyDeleteBookmarkAsync(id, userId, ct: ct);
        await con.AddOutboxMessageAsync(
            await outboxMessageMapper.CreateBookmarkPermanentlyDeletedMessageAsync(bookmark)
        );
        await tran.CommitAsync(ct);

        return Results.NoContent();
    }

    /// <summary>
    /// Update a bookmark by ID
    /// </summary>
    /// <param name="id">The bookmark ID</param>
    /// <param name="updated">The updated bookmark data</param>
    /// <response code="200">The updated bookmark</response>
    /// <response code="404">Bookmark not found</response>
    [ProducesResponseType(typeof(Bookmark), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    internal static async Task<IResult> UpdateBookmark(
        int id,
        Bookmark updated,
        UserId userId,
        [FromKeyedServices("rw")] NpgsqlDataSource ds,
        BookmarkOutboxMessageMapper outboxMessageMapper,
        CancellationToken ct = default
    )
    {
        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        var bookmark = await con.GetBookmarkByIdAsync(id, userId, ct: ct);
        if (bookmark is null)
            return Results.NotFound();

        using var tran = await con.BeginTransactionAsync(ct);
        var newBookmark = bookmark with
        {
            Title = updated.Title,
            Url = updated.Url,
            Summary = updated.Summary,
            UpdatedAt = DateTimeOffset.UtcNow,
            Tags = updated.Tags ?? [],
        };
        await con.UpdateBookmarkAsync(newBookmark, ct: ct);
        await con.AddOutboxMessageAsync(
            await outboxMessageMapper.CreateBookmarkUpdatedMessageAsync(bookmark, newBookmark)
        );
        await tran.CommitAsync(ct);

        return Results.Ok(newBookmark);
    }

    // Response for RemoveTagFromAllBookmarks
    public sealed record RemoveTagResponse(string RemovedTag, int BookmarksUpdated);

    /// <summary>
    /// Remove a tag from all bookmarks
    /// </summary>
    /// <param name="tag">The tag to remove</param>
    /// <response code="200">Tag removal result</response>
    [ProducesResponseType(typeof(RemoveTagResponse), StatusCodes.Status200OK)]
    internal static async Task<IResult> RemoveTagFromAllBookmarks(
        string tag,
        UserId userId,
        [FromKeyedServices("rw")] NpgsqlDataSource ds,
        BookmarkOutboxMessageMapper outboxMessageMapper,
        CancellationToken ct = default
    )
    {
        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        using var tran = await con.BeginTransactionAsync(
            isolationLevel: IsolationLevel.RepeatableRead,
            cancellationToken: ct
        );
        var bookmarksWithTag = (await con.GetBookmarksByTagAsync(tag, userId, forUpdate: true, ct: ct)).ToList();
        var updatedAt = DateTimeOffset.UtcNow;
        await con.RemoveTagFromAllBookmarksAsync(tag, userId, updatedAt, ct: ct);
        await PublishBookmarkUpdatedMessages(tag, outboxMessageMapper, con, bookmarksWithTag, updatedAt);
        await tran.CommitAsync(ct);

        return Results.Ok(new RemoveTagResponse(tag, bookmarksWithTag.Count));

        static async Task PublishBookmarkUpdatedMessages(
            string tag,
            BookmarkOutboxMessageMapper outboxMessageMapper,
            NpgsqlConnection con,
            List<Bookmark> bookmarksWithTag,
            DateTimeOffset updatedAt
        )
        {
            foreach (var oldBookmark in bookmarksWithTag)
                await con.AddOutboxMessageAsync(
                    await outboxMessageMapper.CreateBookmarkUpdatedMessageAsync(
                        oldBookmark,
                        oldBookmark with
                        {
                            Tags = oldBookmark.Tags.Where(t => t != tag).ToList(),
                            UpdatedAt = updatedAt,
                        }
                    )
                );
        }
    }

    public sealed record RenameTagResponse(string RenamedTag, string NewTag, int BookmarksUpdated);

    /// <summary>
    /// Rename a tag on all bookmarks
    /// </summary>
    /// <param name="oldName">The old tag name</param>
    /// <param name="newName">The new tag name</param>
    /// <response code="200">Tag rename result</response>
    [ProducesResponseType(typeof(RenameTagResponse), StatusCodes.Status200OK)]
    internal static async Task<IResult> RenameTagOnAllBookmarks(
        string oldName,
        string newName,
        UserId userId,
        [FromKeyedServices("rw")] NpgsqlDataSource ds,
        BookmarkOutboxMessageMapper outboxMessageMapper,
        CancellationToken ct = default
    )
    {
        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        using var tran = await con.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var bookmarksWithTag = (await con.GetBookmarksByTagAsync(oldName, userId, forUpdate: true, ct: ct)).ToList();
        var affected = await con.RenameTagOnAllBookmarksAsync(oldName, newName, userId, ct: ct);
        await PublishBookmarkUpdatedMessages(oldName, newName, outboxMessageMapper, con, bookmarksWithTag);
        await tran.CommitAsync(ct);

        return Results.Ok(new RenameTagResponse(RenamedTag: oldName, NewTag: newName, BookmarksUpdated: affected));

        static async Task PublishBookmarkUpdatedMessages(
            string oldName,
            string newName,
            BookmarkOutboxMessageMapper outboxMessageMapper,
            NpgsqlConnection con,
            List<Bookmark> bookmarksWithTag
        )
        {
            foreach (var oldBookmark in bookmarksWithTag)
                await con.AddOutboxMessageAsync(
                    await outboxMessageMapper.CreateBookmarkUpdatedMessageAsync(
                        oldBookmark,
                        oldBookmark with
                        {
                            Tags = oldBookmark.Tags.Select(t => t == oldName ? newName : t).Distinct().ToList(),
                            UpdatedAt = DateTimeOffset.UtcNow,
                        }
                    )
                );
        }
    }

    /// <summary>
    /// Import bookmarks from Microsoft Edge bookmarks JSON file
    /// </summary>
    /// <param name="file">The Edge bookmarks JSON file to import</param>
    /// <param name="ds">The database connection</param>
    /// <param name="outboxMessageMapper">The bookmark outbox message mapper</param>
    /// <param name="parser">The Edge bookmarks parser service</param>
    /// <response code="200">List of imported bookmarks</response>
    /// <response code="400">Invalid file format</response>
    [ProducesResponseType(typeof(List<Bookmark>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    internal static async Task<IResult> ImportMsEdgeBookmarks(
        IFormFile file,
        UserId userId,
        [FromKeyedServices("rw")] NpgsqlDataSource ds,
        BookmarkOutboxMessageMapper outboxMessageMapper,
        EdgeBookmarksParser parser,
        CancellationToken ct = default
    )
    {
        if (file == null || file.Length == 0)
            return Results.BadRequest("No file provided");

        var importedBookmarks = new List<Bookmark>();

        try
        {
            using var stream = file.OpenReadStream();
            var bookmarkPayloads = await parser.ParseBookmarksAsync(stream);

            // Insert bookmarks into database
            using var con = await ds.OpenConnectionTraceAsync(ct: ct);
            using var tran = await con.BeginTransactionAsync(ct);

            foreach (var (title, url, tags, createdAt) in bookmarkPayloads)
            {
                var createdAtOffset = new DateTimeOffset(createdAt, TimeSpan.Zero);
                var newBookmark = new Bookmark(
                    Id: -1,
                    Title: title,
                    Url: url,
                    Summary: null,
                    CreatedAt: createdAtOffset,
                    UpdatedAt: DateTimeOffset.UtcNow,
                    Tags: tags,
                    UserId: userId
                );

                var bookmarkId = await con.AddBookmarkAsync(newBookmark, ct: ct);
                newBookmark = newBookmark with { Id = bookmarkId };
                importedBookmarks.Add(newBookmark);

                await con.AddOutboxMessageAsync(
                    await outboxMessageMapper.CreateBookmarkCreatedMessageAsync(newBookmark)
                );
            }

            await tran.CommitAsync(ct);

            return Results.Ok(importedBookmarks);
        }
        catch (JsonException)
        {
            return Results.BadRequest("Invalid JSON format in bookmarks file");
        }
        catch (Exception ex)
        {
            return Results.BadRequest($"Error importing bookmarks: {ex.Message}");
        }
    }

    public sealed record MoveTagResponse(string MovedTag, int BookmarksUpdated);

    /// <summary>
    /// Move a tag to the front or end of the tag list for all bookmarks
    /// </summary>
    /// <param name="tagName">The tag to move</param>
    /// <param name="position">The target position: "front" or "end"</param>
    /// <response code="200">Tag move result</response>
    /// <response code="400">Invalid position parameter</response>
    [ProducesResponseType(typeof(MoveTagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    internal static async Task<IResult> MoveTag(
        string tagName,
        string position,
        UserId userId,
        [FromKeyedServices("rw")] NpgsqlDataSource ds,
        BookmarkOutboxMessageMapper outboxMessageMapper,
        CancellationToken ct = default
    )
    {
        if (position != "front" && position != "end")
            return Results.BadRequest("Position must be 'front' or 'end'");

        using var con = await ds.OpenConnectionTraceAsync(ct: ct);
        using var tran = await con.BeginTransactionAsync(
            isolationLevel: IsolationLevel.RepeatableRead,
            cancellationToken: ct
        );
        var bookmarksWithTag = (await con.GetBookmarksByTagAsync(tagName, userId, forUpdate: true, ct: ct)).ToList();
        var affected =
            position == "front"
                ? await con.MoveTagToFrontOnAllBookmarksAsync(tagName, userId, ct: ct)
                : await con.MoveTagToEndOnAllBookmarksAsync(tagName, userId, ct: ct);
        await PublishBookmarkUpdatedMessages(tagName, position, outboxMessageMapper, con, bookmarksWithTag);
        await tran.CommitAsync(ct);

        return Results.Ok(new MoveTagResponse(MovedTag: tagName, BookmarksUpdated: affected));

        static async Task PublishBookmarkUpdatedMessages(
            string tagName,
            string position,
            BookmarkOutboxMessageMapper outboxMessageMapper,
            NpgsqlConnection con,
            List<Bookmark> bookmarksWithTag
        )
        {
            var updatedAt = DateTimeOffset.UtcNow;
            foreach (var oldBookmark in bookmarksWithTag)
            {
                var newTags = oldBookmark.Tags.Where(t => t != tagName).ToList();
                if (position == "front")
                    newTags.Insert(0, tagName);
                else
                    newTags.Add(tagName);

                await con.AddOutboxMessageAsync(
                    await outboxMessageMapper.CreateBookmarkUpdatedMessageAsync(
                        oldBookmark,
                        oldBookmark with
                        {
                            Tags = newTags,
                            UpdatedAt = updatedAt,
                        }
                    )
                );
            }
        }
    }
}
