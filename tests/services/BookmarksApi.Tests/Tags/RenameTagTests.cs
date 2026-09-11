using System.Net.Http.Json;
using BookmarksApi.Bookmarks;
using BookmarksApi.Outbox;
using Confluent.SchemaRegistry.Serdes;
using static BookmarksApi.Bookmarks.BookmarksEndpoints;

namespace BookmarksApi.Tests.Tags;

[TestClass]
public sealed class RenameTagTests
{
    public TestContext TestContext { get; set; }
    public CancellationToken Token => TestContext.CancellationToken;

    [TestMethod]
    public async Task Test_RenameTagOnAllBookmarks()
    {
        using var fixture = new TestFixture();

        // Arrange: Insert bookmarks with the tag "oldtag"
        var bm1 = fixture.NewBookmarkBuilder().WithTags("oldtag", "keep").Build();
        var bm2 = fixture.NewBookmarkBuilder().WithTags("oldtag").Build();
        var bm3 = fixture.NewBookmarkBuilder().WithTags("othertag").Build();
        var id1 = await fixture.Connection.AddBookmarkAsync(bm1, Token);
        var id2 = await fixture.Connection.AddBookmarkAsync(bm2, Token);
        var id3 = await fixture.Connection.AddBookmarkAsync(bm3, Token);

        using var http = fixture.HttpClientJwt;

        // Act: Rename "oldtag" to "newtag"
        var response = await http.PutAsync($"/api/bookmarks/tags/rename?oldName=oldtag&newName=newtag", null, Token);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RenameTagResponse>(Token);

        // Assert: Response contains correct info
        Assert.AreEqual("oldtag", result!.RenamedTag);
        Assert.AreEqual("newtag", result.NewTag);
        Assert.AreEqual(2, result.BookmarksUpdated);

        // Assert: Bookmarks in DB have updated tags
        var updated1 = await fixture.Connection.GetBookmarkByIdAsync(id1, bm1.UserId, Token);
        var updated2 = await fixture.Connection.GetBookmarkByIdAsync(id2, bm2.UserId, Token);
        var updated3 = await fixture.Connection.GetBookmarkByIdAsync(id3, bm3.UserId, Token);

        CollectionAssert.AreEquivalent(new[] { "newtag", "keep" }, updated1!.Tags);
        CollectionAssert.AreEquivalent(new[] { "newtag" }, updated2!.Tags);
        CollectionAssert.AreEquivalent(new[] { "othertag" }, updated3!.Tags);

        // Assert: Outbox messages for updated bookmarks
        var outbox1 = await fixture.Connection.GetOutboxMessagesByAggregateIdAsync(id1.ToString(), Token);
        var outbox2 = await fixture.Connection.GetOutboxMessagesByAggregateIdAsync(id2.ToString(), Token);
        Assert.Contains(m => m.Type == "bookmark_updated", outbox1);
        Assert.Contains(m => m.Type == "bookmark_updated", outbox2);

        // Protobuf deserialization for one outbox message
        var outboxMessage = outbox1.First(m => m.Type == "bookmark_updated");
        var deserializer = new ProtobufDeserializer<global::Bookmarks.BookmarkUpdated>(null, null);
        var bookmarkUpdated = await deserializer.DeserializeAsync(
            outboxMessage.Payload,
            false,
            new Confluent.Kafka.SerializationContext(Confluent.Kafka.MessageComponentType.Value, "bookmark-updated")
        );
        Assert.IsNotNull(bookmarkUpdated);
        Assert.AreEqual(id1, bookmarkUpdated.OldBookmark.Id);
        CollectionAssert.Contains(bookmarkUpdated.OldBookmark.Tags.ToList(), "oldtag");
        CollectionAssert.DoesNotContain(bookmarkUpdated.NewBookmark.Tags.ToList(), "oldtag");
        CollectionAssert.Contains(bookmarkUpdated.NewBookmark.Tags.ToList(), "newtag");
    }
}
