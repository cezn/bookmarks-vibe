using BookmarksApi.Bookmarks;
using BookmarksApi.Outbox;
using Confluent.SchemaRegistry.Serdes;

namespace BookmarksApi.Tests.Bookmarks;

[TestClass]
public sealed class DeleteBookmarkTests
{
    public TestContext TestContext { get; set; }
    public CancellationToken Token => TestContext.CancellationToken;

    [TestMethod]
    public async Task Test_DeleteBookmark()
    {
        using var fixture = new TestFixture();

        // Arrange: create a bookmark directly in the database
        var dbBookmark = fixture.NewBookmarkBuilder().Build();

        int id = await fixture.Connection.AddBookmarkAsync(dbBookmark, Token);

        // Act: delete the bookmark via API
        var delResp = await fixture.HttpClientJwt.DeleteAsync($"/api/bookmarks/{id}", Token);
        Assert.AreEqual(System.Net.HttpStatusCode.NoContent, delResp.StatusCode);

        // Assert: Bookmark is deleted from DB
        var deleted = await fixture.Connection.GetBookmarkByIdAsync(id, dbBookmark.UserId, Token);
        Assert.IsNull(deleted);

        // Assert: Outbox message
        var outboxMessages = await fixture.Connection.GetOutboxMessagesByAggregateIdAsync(id.ToString(), Token);
        Assert.IsNotNull(outboxMessages);
        Assert.AreEqual(1, outboxMessages.Count());
        var outboxMessage = outboxMessages.First();
        Assert.AreEqual("bookmark_deleted", outboxMessage.Type);
        Assert.AreEqual(id.ToString(), outboxMessage.AggregateId);

        // Protobuf deserialization
        var deserializer = new ProtobufDeserializer<global::Bookmarks.BookmarkDeleted>(null, null);
        var payloadBytes = outboxMessage.Payload;
        var bookmarkDeleted = await deserializer.DeserializeAsync(
            payloadBytes,
            false,
            new Confluent.Kafka.SerializationContext(Confluent.Kafka.MessageComponentType.Value, "bookmark-deleted")
        );
        Assert.IsNotNull(bookmarkDeleted);
        var payload = bookmarkDeleted.Bookmark;
        Assert.IsNotNull(payload);
        Assert.AreEqual(id, payload.Id);
        Assert.AreEqual(dbBookmark.Title, payload.Title);
        Assert.AreEqual(dbBookmark.Url, payload.Url);
        Assert.AreEqual(dbBookmark.Summary, payload.Summary);
        CollectionAssert.AreEquivalent(dbBookmark.Tags.ToList(), payload.Tags.ToList());
    }
}
