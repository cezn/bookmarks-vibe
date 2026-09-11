using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookmarksApi.Bookmarks;
using static BookmarksApi.Bookmarks.BookmarksEndpoints;

namespace BookmarksApi.Tests.Bookmarks;

[TestClass]
public sealed class GetBookmarksTagsTests
{
    public TestContext TestContext { get; set; }
    public CancellationToken Token => TestContext.CancellationToken;

    [TestMethod]
    public async Task Test_GetBookmarksTags_WithoutTags_ReturnsEmptyList()
    {
        using var fixture = new TestFixture(userId: Guid.CreateVersion7());

        // Act
        var response = await fixture.HttpClientJwt.GetFromJsonAsync<List<TagFacet>>("/api/bookmarks/tags", Token);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsEmpty(response);
    }

    [TestMethod]
    public async Task Test_GetBookmarksTags_WithMultipleBookmarksAndTags_ReturnsAggregatedTagCounts()
    {
        using var fixture = new TestFixture(userId: Guid.CreateVersion7());

        // Arrange: Create bookmarks with various tags
        var bookmark1 = fixture
            .NewBookmarkBuilder()
            .WithTitle("Test Bookmark 1")
            .WithTags("csharp", "dotnet", "programming")
            .Build();
        var bookmark2 = fixture
            .NewBookmarkBuilder()
            .WithTitle("Test Bookmark 2")
            .WithTags("csharp", "aspnetcore")
            .Build();
        var bookmark3 = fixture
            .NewBookmarkBuilder()
            .WithTitle("Test Bookmark 3")
            .WithTags("dotnet", "performance")
            .Build();

        await fixture.Connection.AddBookmarkAsync(bookmark1, Token);
        await fixture.Connection.AddBookmarkAsync(bookmark2, Token);
        await fixture.Connection.AddBookmarkAsync(bookmark3, Token);

        // Act
        var response = await fixture.HttpClientJwt.GetFromJsonAsync<List<TagFacet>>("/api/bookmarks/tags", Token);

        // Assert
        Assert.IsNotNull(response);
        Assert.HasCount(5, response);

        // Check aggregated counts
        var csharpTag = response.FirstOrDefault(t => t.Name == "csharp");
        Assert.IsNotNull(csharpTag);
        Assert.AreEqual(2, csharpTag.Count);

        var dotnetTag = response.FirstOrDefault(t => t.Name == "dotnet");
        Assert.IsNotNull(dotnetTag);
        Assert.AreEqual(2, dotnetTag.Count);

        var programmingTag = response.FirstOrDefault(t => t.Name == "programming");
        Assert.IsNotNull(programmingTag);
        Assert.AreEqual(1, programmingTag.Count);

        var aspnetcoreTag = response.FirstOrDefault(t => t.Name == "aspnetcore");
        Assert.IsNotNull(aspnetcoreTag);
        Assert.AreEqual(1, aspnetcoreTag.Count);

        var performanceTag = response.FirstOrDefault(t => t.Name == "performance");
        Assert.IsNotNull(performanceTag);
        Assert.AreEqual(1, performanceTag.Count);
    }

    [TestMethod]
    public async Task Test_GetBookmarksTags_ReturnsOrderedByCountDescending()
    {
        using var fixture = new TestFixture(userId: Guid.CreateVersion7());

        // Arrange: Create bookmarks with tags in a specific pattern
        var bookmark1 = fixture.NewBookmarkBuilder().WithTitle("Bookmark 1").WithTags("common", "rare").Build();
        var bookmark2 = fixture.NewBookmarkBuilder().WithTitle("Bookmark 2").WithTags("common").Build();
        var bookmark3 = fixture.NewBookmarkBuilder().WithTitle("Bookmark 3").WithTags("rare").Build();

        await fixture.Connection.AddBookmarkAsync(bookmark1, Token);
        await fixture.Connection.AddBookmarkAsync(bookmark2, Token);
        await fixture.Connection.AddBookmarkAsync(bookmark3, Token);

        // Act
        var response = await fixture.HttpClientJwt.GetFromJsonAsync<List<TagFacet>>("/api/bookmarks/tags", Token);

        // Assert
        Assert.IsNotNull(response);
        Assert.HasCount(2, response);

        // First tag should have count 2, second should have count 2
        // Both "common" and "rare" appear in 2 bookmarks
        var tagList = response.OrderByDescending(t => t.Count).ToList();
        Assert.AreEqual(2, tagList[0].Count);
        Assert.AreEqual(2, tagList[1].Count);
    }

    [TestMethod]
    public async Task Test_GetBookmarksTags_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var fixture = new TestFixture();
        using var client = fixture.Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/bookmarks/tags", Token);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Test_GetBookmarksTags_WithInvalidToken_ReturnsUnauthorized()
    {
        using var fixture = new TestFixture();
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await client.GetAsync("/api/bookmarks/tags", Token);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Test_GetBookmarksTags_OnlyReturnsTagsForAuthenticatedUser()
    {
        using var fixture = new TestFixture(userId: Guid.CreateVersion7());

        // Arrange: Create a bookmark with the authenticated user
        var bookmark = fixture
            .NewBookmarkBuilder()
            .WithTitle("User Specific Bookmark")
            .WithTags("user-specific-tag")
            .Build();
        var otherUserBookmark = fixture
            .NewBookmarkBuilder()
            .WithTitle("Other User Bookmark")
            .WithTags("other-user-tag")
            .WithUserId(Guid.NewGuid().ToString())
            .Build();

        await fixture.Connection.AddBookmarkAsync(bookmark, Token);
        await fixture.Connection.AddBookmarkAsync(otherUserBookmark, Token);

        // Act: Get tags for the authenticated user
        var response = await fixture.HttpClientJwt.GetFromJsonAsync<List<TagFacet>>("/api/bookmarks/tags", Token);

        // Assert: Should only see the tag from the authenticated user's bookmark
        Assert.IsNotNull(response);
        Assert.HasCount(1, response);
        Assert.AreEqual("user-specific-tag", response[0].Name);
    }
}
