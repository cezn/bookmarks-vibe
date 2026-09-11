using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookmarksApi.Bookmarks;
using static BookmarksApi.Bookmarks.BookmarksEndpoints;

namespace BookmarksApi.Tests.Bookmarks;

[TestClass]
public sealed class GetBookmarksTests
{
    public TestContext TestContext { get; set; }
    public CancellationToken Token => TestContext.CancellationToken;

    [TestMethod]
    public async Task Test_GetBookmarks_WithoutQueryParameters_ReturnsOk()
    {
        using var fixture = new TestFixture();

        // Arrange

        // Act
        var response = await fixture.HttpClientJwt.GetAsync("/api/bookmarks", Token);

        // Assert
        Assert.IsTrue(response.IsSuccessStatusCode, $"Expected OK, got {response.StatusCode}");
    }

    [TestMethod]
    public async Task Test_GetBookmakrs()
    {
        using var fixture = new TestFixture();

        // Arrange
        var dbBookmark = fixture.NewBookmarkBuilder().Build();
        await fixture.Connection.AddBookmarkAsync(dbBookmark, Token);

        // Act
        var response = await fixture.HttpClientJwt.GetFromJsonAsync<GetBookmarksResponse>(
            $"/api/bookmarks?q={Uri.EscapeDataString(dbBookmark.Title)}&sort=createdAt&limit=10",
            Token
        );

        // Assert
        Assert.IsNotNull(response);
        var bookmarks = response.Bookmarks;
        var bookmark = bookmarks.FirstOrDefault(b => b.Title == dbBookmark.Title);
        Assert.IsNotNull(bookmark);
        Assert.AreEqual(dbBookmark.Title, bookmark.Title);
        Assert.AreEqual(dbBookmark.Url, bookmark.Url);
        Assert.AreEqual(dbBookmark.Summary, bookmark.Summary);
        CollectionAssert.AreEquivalent(dbBookmark.Tags.ToList(), bookmark.Tags.ToList());
        Assert.AreEqual(dbBookmark.CreatedAt, bookmark.CreatedAt);
        Assert.AreEqual(dbBookmark.UpdatedAt, bookmark.UpdatedAt);
    }

    [TestMethod]
    [DataRow("createdAt")]
    [DataRow("updatedAt")]
    [DataRow("id")]
    [DataRow("title")]
    public async Task Test_GetBookmarks_CursorPagination(string sortField)
    {
        using var fixture = new TestFixture();

        // Arrange: Insert 3 bookmarks with unique titles for pagination
        var guidPrefix = Guid.CreateVersion7().ToString();
        var titles = new[] { "CursorTest1", "CursorTest2", "CursorTest3" }.Select(t => $"{guidPrefix}_{t}").ToArray();
        var bookmarks = titles
            .Select(
                (title, i) =>
                    fixture
                        .NewBookmarkBuilder()
                        .WithTitle(title)
                        .WithUrl($"https://example.com/{i}")
                        .WithSummary($"Summary {i}")
                        .CreatedAt(new DateTimeOffset(2024, 1, 1, 12, 0, i, TimeSpan.Zero))
                        .UpdatedAt(new DateTimeOffset(2024, 1, 1, 12, 0, i, TimeSpan.Zero))
                        .WithTags("cursor")
                        .Build()
            )
            .ToList();

        foreach (var bm in bookmarks)
            await fixture.Connection.AddBookmarkAsync(bm, Token);

        // Act: Get first 2 bookmarks
        var response1 = await fixture.HttpClientJwt.GetFromJsonAsync<GetBookmarksResponse>(
            $"/api/bookmarks?q={Uri.EscapeDataString(guidPrefix)}&sort={sortField}&limit=2&sortDirection=ASC",
            Token
        );
        Assert.IsNotNull(response1);
        Assert.HasCount(2, response1.Bookmarks);

        // Use nextCursor to get the next page
        var nextCursor = response1.NextCursor;
        Assert.IsNotNull(nextCursor);

        var response2 = await fixture.HttpClientJwt.GetFromJsonAsync<GetBookmarksResponse>(
            $"/api/bookmarks?q={Uri.EscapeDataString(guidPrefix)}&sort={sortField}&limit=2&sortDirection=ASC&cursor={Uri.EscapeDataString(nextCursor)}",
            Token
        );
        Assert.IsNotNull(response2);
        Assert.HasCount(1, response2.Bookmarks);

        // Assert: All bookmarks are returned in order, no duplicates
        var allTitles = response1.Bookmarks.Concat(response2.Bookmarks).Select(b => b.Title).ToList();
        CollectionAssert.AreEquivalent(titles, allTitles);
    }

    [TestMethod]
    public async Task Test_GetBookmarks_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var fixture = new TestFixture();
        using var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/bookmarks", Token);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Test_GetBookmarks_WithInvalidToken_ReturnsUnauthorized()
    {
        using var fixture = new TestFixture();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await client.GetAsync("/api/bookmarks", Token);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Test_GetBookmarks_WithWrongAuthenticationSchema_ReturnsUnauthorized()
    {
        using var fixture = new TestFixture();

        var response = await fixture.HttpClientApiKey.GetAsync("/api/bookmarks", Token);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
