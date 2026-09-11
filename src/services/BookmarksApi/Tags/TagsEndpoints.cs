using BookmarksApi.Idempotency;
using BookmarksApi.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Npgsql;

namespace BookmarksApi.Tags;

public enum TagSort
{
    Id,
    Name,
    UsageCount,
    CreatedAt,
}

public static class TagsWebApplicationExtensions
{
    public static void MapTagEndpoints(this WebApplication app)
    {
        app.MapGet("/api/tags", TagsEndpoints.GetTags);
        app.MapPost("/api/tags/bulk/add", TagsEndpoints.AddTags)
            .RequireAuthorization("RequireApiKey")
            .WithMetadata(new IdempotentAttribute());
        app.MapPost("/api/tags/bulk/remove", TagsEndpoints.RemoveTagsByName)
            .RequireAuthorization("RequireApiKey")
            .WithMetadata(new IdempotentAttribute());
        app.MapHub<TagsHub>("/api/tags/events");
    }
}

public static class TagsEndpoints
{
    public sealed record GetTagsResponse(IReadOnlyList<Tag> Tags, string? NextCursor);

    /// <summary>
    /// Get user's tags with optional search query, sorting, and pagination
    /// </summary>
    /// <param name="q">Optional search query string.</param>
    /// <param name="sort">Optional sort field. Values: id, name, usageCount, createdAt. Default: id</param>
    /// <param name="sortDirection">Optional sort direction. Values: asc, desc. Default: desc</param>
    /// <param name="limit">Maximum number of tags to return per page. Default: 20</param>
    /// <param name="cursor">Optional cursor for pagination. Use the NextCursor from a previous response to get the next page of results. Encoded as a base64 string.</param>
    /// <response code="200">List of tags</response>
    [ProducesResponseType(typeof(GetTagsResponse), StatusCodes.Status200OK)]
    internal static async Task<IResult> GetTags(
        UserId userId,
        [FromKeyedServices("ro")] NpgsqlDataSource ds,
        string? q,
        CaseInsensitive<TagSort>? sort,
        CaseInsensitive<SortDirection>? sortDirection,
        int limit = 20,
        Cursor? cursor = null,
        CancellationToken ct = default
    )
    {
        sort ??= new(TagSort.Id);
        sortDirection ??= new(SortDirection.Desc);
        limit = limit == 0 ? 20 : limit;

        using var con = await ds.OpenConnectionTraceAsync(ct: ct);

        var tags = (
            await con.SearchTagsCursorAsync(
                userId: userId,
                sort: sort.Value,
                cursor: cursor,
                limit: limit,
                sortDirection: sortDirection.Value,
                q: q,
                ct: ct
            )
        ).ToList();

        Cursor? nextCursor =
            tags.Count == limit && tags.Last() is Tag last
                ? new Cursor(
                    SortValue: sort.Value switch
                    {
                        TagSort.Name => last.Name,
                        TagSort.UsageCount => last.UsageCount.ToString(),
                        TagSort.CreatedAt => last.CreatedAt.ToString("u"),
                        TagSort.Id or _ => last.Id.ToString(),
                    },
                    Id: last.Id
                )
                : null;

        return Results.Ok(new GetTagsResponse(tags, nextCursor?.ToBase64()));
    }

    public sealed record AddTagsRequest(string[] TagNames);

    /// <summary>
    /// Add multiple tags
    /// </summary>
    /// <param name="request">The request containing tag names to add</param>
    /// <response code="200">The added tags</response>
    /// <response code="401">Unauthorized</response>
    [ProducesResponseType(typeof(Tag[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    internal static async Task<IResult> AddTags(
        AddTagsRequest request,
        UserId userId,
        IHubContext<TagsHub, ITagsClient> hubContext,
        [FromKeyedServices("rw")] NpgsqlConnection con,
        CancellationToken ct = default
    )
    {
        var tags = request.TagNames.Select(name => new Tag(-1, name, 0, DateTimeOffset.UtcNow, userId)).ToArray();
        tags = await con.AddTagsAsync(tags, userId, ct);

        foreach (var tag in tags)
            await hubContext.Clients.User(tag.UserId).OnTagUpdated(tag);

        return Results.Ok(tags);
    }

    public sealed record RemoveTagsByNameRequest(string[] TagNames);

    /// <summary>
    /// Remove tags by name
    /// </summary>
    /// <param name="request">The request containing tag names to remove</param>
    /// <response code="204">Tags removed</response>
    /// <response code="401">Unauthorized</response>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    internal static async Task<IResult> RemoveTagsByName(
        RemoveTagsByNameRequest request,
        UserId userId,
        [FromKeyedServices("rw")] NpgsqlConnection con,
        CancellationToken ct = default
    )
    {
        await con.RemoveTagsByNameAsync(request.TagNames, userId, ct);
        return Results.NoContent();
    }
}
