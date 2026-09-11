using System.Net.Http.Json;
using BookmarksApi.Bookmarks;
using BookmarksApi.Outbox;
using BookmarksApi.Tests.Utils;
using static BookmarksApi.Bookmarks.BookmarksEndpoints;

namespace BookmarksApi.Tests.Bookmarks;

[TestClass]
public sealed class DeleteTagFromAllBookmarksTests
{
    public TestContext TestContext { get; set; }
    public CancellationToken Token => TestContext.CancellationToken;

    [TestMethod]
    public async Task Test_DeleteTagFromAllBookmarks()
    {
        using var fixture = new TestFixture();

        // Arrange
        var tagToRemove = "remove-me";
        var bookmarks = new[]
        {
            new BookmarkBuilder().WithTags(tagToRemove, "x").Build(),
            new BookmarkBuilder().WithTags(tagToRemove).Build(),
            new BookmarkBuilder().WithTags("other").Build(),
        };

        // Insert bookmarks
        var createdBookmarks = new List<Bookmark>();
        foreach (var bm in bookmarks)
        {
            var resp = await fixture.HttpClientJwt.PostAsJsonAsync("/api/bookmarks", bm, Token);
            resp.EnsureSuccessStatusCode();
            var created = await resp.Content.ReadFromJsonAsync<Bookmark>(Token);
            Assert.IsNotNull(created);
            createdBookmarks.Add(created!);
        }

        // Act
        var deleteResp = await fixture.HttpClientJwt.DeleteAsync($"/api/bookmarks/tags/{tagToRemove}", Token);
        deleteResp.EnsureSuccessStatusCode();
        var result = await deleteResp.Content.ReadFromJsonAsync<RemoveTagResponse>(Token);
        Assert.IsNotNull(result);
        Assert.AreEqual(tagToRemove, result.RemovedTag);

        // Assert bookmarks in DB
        foreach (var bm in createdBookmarks)
        {
            var dbBookmark = await fixture.Connection.GetBookmarkByIdAsync(bm.Id, bm.UserId, Token);
            Assert.IsNotNull(dbBookmark);
            Assert.DoesNotContain(tagToRemove, dbBookmark.Tags);
            // Bookmarks that didn't have the tag should remain unchanged
            if (bm.Tags.Contains(tagToRemove))
            {
                Assert.IsGreaterThan(bm.UpdatedAt, dbBookmark.UpdatedAt);
            }
            else
            {
                Assert.AreEqual(
                    bm.UpdatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss"),
                    dbBookmark.UpdatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss")
                );
            }
        }

        // Assert outbox messages for updated bookmarks
        foreach (var bm in createdBookmarks.Where(b => b.Tags.Contains(tagToRemove)))
        {
            var outboxMessages = await fixture.Connection.GetOutboxMessagesByAggregateIdAsync(bm.Id.ToString(), Token);
            Assert.Contains(m => m.Type == "bookmark_updated", outboxMessages);
        }
    }
}
