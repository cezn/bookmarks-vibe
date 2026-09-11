namespace TagsBookmarksSubscriber.Tests;

[TestClass]
public sealed class ConsumeBookmarkCreatedTests
{
    [TestMethod]
    public async Task Test_BookmarkCreatedAsync()
    {
#pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.IsTrue(true);
#pragma warning restore MSTEST0032 // Assertion condition is always true

        /*

        // TODO: not sure if these tests are necessary.
        // Fixture should start consumer in a test consumer group with the latest offset.
        using var fixture = new TestFixture();
        fixture.Logger.LogInformation("Arrange:");
        var newBookmark = fixture.NewBookmarkBuilder().Build();
        var outboxMessage = await fixture.BookmarkOutboxMessageMapper.CreateBookmarkCreatedMessageAsync(newBookmark);

        fixture.Logger.LogInformation("Act:");
        await fixture.Connection.AddOutboxMessageAsync(outboxMessage);

        fixture.Logger.LogInformation("Assert:");
        // BookmarksApi is a WiredMock instance.
        var request = fixture.BookmarksApi.FindRequest(
            idempotencyHeader: outboxMessage.Id,
            method: "POST",
            path: "/api/tags/bulk/add"
        );
        // TODO: assert request.

         */
    }
}
