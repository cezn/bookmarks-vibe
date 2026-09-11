using System.Net.Http.Json;
using BookmarksApi.Bookmarks;
using BookmarksApi.Outbox;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.AspNetCore.Http;

namespace BookmarksApi.Tests.Bookmarks;

[TestClass]
public sealed class PutBookmarkTests
{
    public TestContext TestContext { get; set; }
    public CancellationToken Token => TestContext.CancellationToken;

    [TestMethod]
    public async Task Test_PutBookmark_With_Malformed_Json()
    {
        using var fixture = new TestFixture();

        // Arrange: Create a bookmark first
        var originalBookmark = fixture.NewBookmarkBuilder().Build();
        var createResponse = await fixture.HttpClientJwt.PostAsJsonAsync("/api/bookmarks", originalBookmark, Token);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<Bookmark>(Token);
        Assert.IsNotNull(created);

        // Arrange: Prepare malformed JSON (missing closing brace)
        using var content = new StringContent(
            """
            {
                "id": 0,
                "title": "Malformed JSON",
                "url": "https://example.com",
                "summary": "A test bookmark",
                "createdAt": "2023-01-01T00:00:00Z",
                "updatedAt": "2023-01-01T00:00:00Z",
                "tags": ["test", "bookmark"]
            """, // <-- missing closing brace
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await fixture.HttpClientJwt.PutAsync($"/api/bookmarks/{created.Id}", content, Token);

        // Assert: Deserialization should fail and the API should return 400 Bad Request
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.MatchesRegex(
            """
            {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Invalid JSON","status":400,"detail":"The JSON payload is in an incorrect format.","traceId":"00-[a-z0-9]{32}-[a-z0-9]{16}-01"}
            """,
            await response.Content.ReadAsStringAsync(Token)
        );
    }

    [TestMethod]
    public async Task Test_PutBookmark_With_Invalid_Payload()
    {
        using var fixture = new TestFixture();

        // Arrange: Create a bookmark first
        var originalBookmark = fixture.NewBookmarkBuilder().Build();
        var createResponse = await fixture.HttpClientJwt.PostAsJsonAsync("/api/bookmarks", originalBookmark, Token);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<Bookmark>(Token);
        Assert.IsNotNull(created);

        // Arrange: Prepare invalid payload (CreatedAt is not a valid date)
        using var content = new StringContent(
            """
            {
                "id": 0,
                "title": "Bookmark with invalid CreatedAt",
                "url": "https://example.com",
                "summary": "A test bookmark",
                "createdAt": "not-a-date",
                "updatedAt": "2023-01-01T00:00:00Z",
                "tags": ["test", "bookmark"]
            }
            """,
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await fixture.HttpClientJwt.PutAsync($"/api/bookmarks/{created.Id}", content, Token);

        // Assert: Deserialization should fail and the API should return 400 Bad Request
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.MatchesRegex(
            """
            {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Invalid JSON","status":400,"detail":"The JSON payload is in an incorrect format.","traceId":"00-[a-z0-9]{32}-[a-z0-9]{16}-01"}
            """,
            await response.Content.ReadAsStringAsync(Token)
        );

        // Ensure the bookmark was not updated
        var dbBookmark = await fixture.Connection.GetBookmarkByIdAsync(created.Id, created.UserId, Token);
        Assert.IsNotNull(dbBookmark);
        Assert.AreEqual(originalBookmark.Title, dbBookmark.Title);
    }

    [TestMethod]
    public async Task Test_PutBookmark()
    {
        using var fixture = new TestFixture();

        // Arrange: Create a bookmark first
        var originalBookmark = fixture.NewBookmarkBuilder().Build();
        var createResponse = await fixture.HttpClientJwt.PostAsJsonAsync("/api/bookmarks", originalBookmark, Token);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<Bookmark>(Token);
        Assert.IsNotNull(created);

        // Arrange: Prepare updated bookmark
        var updatedBookmark = fixture
            .NewBookmarkBuilder()
            .WithId(created.Id)
            .WithTitle("Updated Title")
            .WithUrl("https://updated-example.com")
            .WithSummary("Updated summary")
            .WithTags("updated", "test")
            .WithUserId(created.UserId)
            .CreatedAt(created.CreatedAt)
            .Build();

        // Act
        var response = await fixture.HttpClientJwt.PutAsJsonAsync(
            $"/api/bookmarks/{created.Id}",
            updatedBookmark,
            Token
        );
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<Bookmark>(Token);

        // Assert
        Assert.IsNotNull(updated);
        Assert.AreEqual(created.Id, updated.Id);
        Assert.AreEqual(updatedBookmark.Title, updated.Title);
        Assert.AreEqual(updatedBookmark.Url, updated.Url);
        Assert.AreEqual(updatedBookmark.Summary, updated.Summary);
        CollectionAssert.AreEquivalent(updatedBookmark.Tags.ToList(), updated.Tags.ToList());
        Assert.IsLessThan(1000, Math.Abs((created.CreatedAt - updated.CreatedAt).TotalMilliseconds)); // CreatedAt should not change
        Assert.IsGreaterThan(created.UpdatedAt, updated.UpdatedAt); // UpdatedAt should be newer

        // Assert database bookmark
        var dbBookmark = await fixture.Connection.GetBookmarkByIdAsync(updated.Id, updated.UserId, Token);
        Assert.IsNotNull(dbBookmark);
        Assert.AreEqual(updated.Title, dbBookmark.Title);
        Assert.AreEqual(updated.Url, dbBookmark.Url);
        Assert.AreEqual(updated.Summary, dbBookmark.Summary);
        CollectionAssert.AreEquivalent(updated.Tags.ToList(), dbBookmark.Tags.ToList());

        // Assert outbox
        var outboxMessages = await fixture.Connection.GetOutboxMessagesByAggregateIdAsync(
            dbBookmark.Id.ToString(),
            Token
        );
        Assert.IsNotNull(outboxMessages);
        Assert.AreEqual(2, outboxMessages.Count()); // One for create, one for update
        var updateMessage = outboxMessages.First(m => m.Type == "bookmark_updated");
        Assert.AreEqual("bookmark_updated", updateMessage.Type);
        Assert.AreEqual(dbBookmark.Id.ToString(), updateMessage.AggregateId);
        Assert.AreEqual(fixture.UserId.ToString(), updateMessage.UserId);

        // Protobuf deserialization
        var deserializer = new ProtobufDeserializer<global::Bookmarks.BookmarkUpdated>(null, null);
        var bookmarkUpdated = await deserializer.DeserializeAsync(
            updateMessage.Payload,
            false,
            new Confluent.Kafka.SerializationContext(Confluent.Kafka.MessageComponentType.Value, "bookmark-updated")
        );
        Assert.IsNotNull(bookmarkUpdated);
        var payload = bookmarkUpdated.NewBookmark;
        Assert.IsNotNull(payload);
        Assert.AreEqual(updated.Id, payload.Id);
        Assert.AreEqual(updated.Title, payload.Title);
        Assert.AreEqual(updated.Url, payload.Url);
        Assert.AreEqual(updated.Summary, payload.Summary);
        CollectionAssert.AreEquivalent(updated.Tags.ToList(), payload.Tags.ToList());
    }

    [TestMethod]
    public async Task Test_PutBookmark_Without_Title()
    {
        using var fixture = new TestFixture();

        // Arrange: Create a bookmark first
        var originalBookmark = fixture.NewBookmarkBuilder().Build();
        var createResponse = await fixture.HttpClientJwt.PostAsJsonAsync("/api/bookmarks", originalBookmark, Token);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<Bookmark>(Token);
        Assert.IsNotNull(created);

        // Arrange: Prepare payload without Title
        using var content = new StringContent(
            """
            {
                "id": 0,
                "url": "https://example.com",
                "summary": "A test bookmark",
                "tags": ["test", "bookmark"]
            }
            """,
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await fixture.HttpClientJwt.PutAsync($"/api/bookmarks/{created.Id}", content, Token);

        // Assert: Validation should fail and the API should return 400 Bad Request
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var responseBody = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(Token);
        Assert.IsNotNull(responseBody);
        Assert.AreEqual(400, responseBody.Status);
        Assert.IsTrue(responseBody.Errors.ContainsKey("title"));
        CollectionAssert.Contains(responseBody.Errors["title"], "The Title field is required.");
    }

    [TestMethod]
    public async Task Test_PutBookmark_Without_Url()
    {
        using var fixture = new TestFixture();

        // Arrange: Create a bookmark first
        var originalBookmark = fixture.NewBookmarkBuilder().Build();
        var createResponse = await fixture.HttpClientJwt.PostAsJsonAsync("/api/bookmarks", originalBookmark, Token);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<Bookmark>(Token);
        Assert.IsNotNull(created);

        // Arrange: Prepare payload without Url
        using var content = new StringContent(
            """
            {
                "id": 0,
                "title": "Bookmark without Url",
                "summary": "A test bookmark",
                "createdAt": "2023-01-01T00:00:00Z",
                "updatedAt": "2023-01-01T00:00:00Z",
                "tags": ["test", "bookmark"]
            }
            """,
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await fixture.HttpClientJwt.PutAsync($"/api/bookmarks/{created.Id}", content, Token);

        // Assert: Validation should fail and the API should return 400 Bad Request
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var responseBody = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(Token);
        Assert.IsNotNull(responseBody);
        Assert.AreEqual(400, responseBody.Status);
        Assert.IsTrue(responseBody.Errors.ContainsKey("url"));
        CollectionAssert.Contains(responseBody.Errors["url"], "The Url field is required.");
    }

    [TestMethod]
    public async Task Test_PutBookmark_NonExistent()
    {
        using var fixture = new TestFixture();

        // Arrange: Prepare updated bookmark for non-existent ID
        var updatedBookmark = fixture.NewBookmarkBuilder().WithId(99999).Build();

        // Act
        var response = await fixture.HttpClientJwt.PutAsJsonAsync("/api/bookmarks/99999", updatedBookmark, Token);

        // Assert: Should return 404 Not Found
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Test_PutBookmark_Ignores_CreatedAt_And_UpdatedAt()
    {
        using var fixture = new TestFixture();

        // Arrange: Create a bookmark first
        var originalBookmark = fixture.NewBookmarkBuilder().Build();
        var createResponse = await fixture.HttpClientJwt.PostAsJsonAsync("/api/bookmarks", originalBookmark, Token);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<Bookmark>(Token);
        Assert.IsNotNull(created);

        // Arrange: Prepare payload with CreatedAt and UpdatedAt set to old dates
        var oldCreatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var oldUpdatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var content = new StringContent(
            $$"""
            {
                "id": 0,
                "title": "Updated Test Bookmark",
                "url": "https://updated-example.com",
                "summary": "Updated test bookmark",
                "createdAt": "{{oldCreatedAt:o}}",
                "updatedAt": "{{oldUpdatedAt:o}}",
                "tags": ["updated", "test"]
            }
            """,
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await fixture.HttpClientJwt.PutAsync($"/api/bookmarks/{created.Id}", content, Token);
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<Bookmark>(Token);

        // Assert: CreatedAt should remain the same, UpdatedAt should be set by the API
        Assert.IsNotNull(updated);
        Assert.IsLessThan(1000, Math.Abs((created.CreatedAt - updated.CreatedAt).TotalMilliseconds)); // CreatedAt should not change
        Assert.AreNotEqual(oldUpdatedAt, updated.UpdatedAt); // UpdatedAt should not be the old value
        // UpdatedAt should be recent (within the last minute)
        var now = DateTimeOffset.UtcNow;
        Assert.IsLessThan(1, (now - updated.UpdatedAt).TotalMinutes);
    }
}
