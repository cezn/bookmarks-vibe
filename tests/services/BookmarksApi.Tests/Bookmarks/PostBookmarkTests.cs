using System.Net.Http.Json;
using BookmarksApi.Bookmarks;
using BookmarksApi.Outbox;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.AspNetCore.Http;

namespace BookmarksApi.Tests.Bookmarks;

[TestClass]
public sealed class PostBookmarkTests
{
    public TestContext TestContext { get; set; }
    public CancellationToken Token => TestContext.CancellationToken;

    [TestMethod]
    public async Task Test_PostBookmark_With_Malformed_Json()
    {
        using var fixture = new TestFixture();

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
        var response = await fixture.HttpClientJwt.PostAsync("/api/bookmarks", content, Token);

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
    public async Task Test_PostBookmark_With_Invalid_Payload()
    {
        using var fixture = new TestFixture();

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
        var response = await fixture.HttpClientJwt.PostAsync("/api/bookmarks", content, Token);

        // Assert: Deserialization should fail and the API should return 400 Bad Request
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.MatchesRegex(
            """
            {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Invalid JSON","status":400,"detail":"The JSON payload is in an incorrect format.","traceId":"00-[a-z0-9]{32}-[a-z0-9]{16}-01"}
            """,
            await response.Content.ReadAsStringAsync(Token)
        );

        // Ensure nothing was persisted to the database (no bookmark with the title)
        var bookmarks = await fixture.Connection.SearchBookmarksCursorAsync(
            q: "Bookmark with invalid CreatedAt",
            userId: "test-user-id",
            ct: Token
        );
        Assert.DoesNotContain(bookmarks.Select(b => b.Title), "Bookmark with invalid CreatedAt");
    }

    [TestMethod]
    public async Task Test_PostBookmark()
    {
        using var fixture = new TestFixture();

        // Arrange
        var newBookmark = fixture.NewBookmarkBuilder().Build();

        // Act
        var response = await fixture.HttpClientJwt.PostAsJsonAsync("/api/bookmarks", newBookmark, Token);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<Bookmark>(Token);

        // Assert
        Assert.IsNotNull(created);
        Assert.AreNotEqual(-1, created.Id);
        Assert.AreEqual(newBookmark.Title, created.Title);
        Assert.AreEqual(newBookmark.Url, created.Url);
        Assert.AreEqual(newBookmark.Summary, created.Summary);
        CollectionAssert.AreEquivalent(newBookmark.Tags.ToList(), created.Tags.ToList());
        Assert.IsGreaterThan(DateTimeOffset.MinValue, created.CreatedAt);
        Assert.IsGreaterThan(DateTimeOffset.MinValue, created.UpdatedAt);
        Assert.AreEqual(fixture.UserId.ToString(), created.UserId);

        // Assert database bookmark
        var dbBookmark = await fixture.Connection.GetBookmarkByIdAsync(created.Id, created.UserId, Token);
        Assert.IsNotNull(dbBookmark);
        Assert.AreEqual(created.Title, dbBookmark.Title);
        Assert.AreEqual(created.Url, dbBookmark.Url);
        Assert.AreEqual(created.Summary, dbBookmark.Summary);
        CollectionAssert.AreEquivalent(created.Tags.ToList(), dbBookmark.Tags.ToList());

        // Assert outbox
        var outboxMessages = await fixture.Connection.GetOutboxMessagesByAggregateIdAsync(
            dbBookmark.Id.ToString(),
            Token
        );
        Assert.IsNotNull(outboxMessages);
        Assert.AreEqual(1, outboxMessages.Count());
        var outboxMessage = outboxMessages.First();
        Assert.AreEqual("bookmark_created", outboxMessage.Type);
        Assert.AreEqual(dbBookmark.Id.ToString(), outboxMessage.AggregateId);
        Assert.AreEqual(fixture.UserId.ToString(), outboxMessage.UserId);

        // Protobuf deserialization
        var deserializer = new ProtobufDeserializer<global::Bookmarks.BookmarkCreated>(null, null);
        var payloadBytes = outboxMessage.Payload;
        var bookmarkCreated = await deserializer.DeserializeAsync(
            payloadBytes,
            false,
            new Confluent.Kafka.SerializationContext(Confluent.Kafka.MessageComponentType.Value, "bookmark-created")
        );
        Assert.IsNotNull(bookmarkCreated);
        var payload = bookmarkCreated.Bookmark;
        Assert.IsNotNull(payload);
        Assert.AreEqual(created.Id, payload.Id);
        Assert.AreEqual(created.Title, payload.Title);
        Assert.AreEqual(created.Url, payload.Url);
        Assert.AreEqual(created.Summary, payload.Summary);
        CollectionAssert.AreEquivalent(created.Tags.ToList(), payload.Tags.ToList());
    }

    [TestMethod]
    public async Task Test_PostBookmark_Without_Title()
    {
        using var fixture = new TestFixture();

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
        var response = await fixture.HttpClientJwt.PostAsync("/api/bookmarks", content, Token);

        // Assert: Validation should fail and the API should return 400 Bad Request
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var responseBody = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(Token);
        Assert.IsNotNull(responseBody);
        Assert.AreEqual(400, responseBody.Status);
        Assert.IsTrue(responseBody.Errors.ContainsKey("title"));
        CollectionAssert.Contains(responseBody.Errors["title"], "The Title field is required.");
    }

    [TestMethod]
    public async Task Test_PostBookmark_Without_Url()
    {
        using var fixture = new TestFixture();

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
        var response = await fixture.HttpClientJwt.PostAsync("/api/bookmarks", content, Token);

        // Assert: Validation should fail and the API should return 400 Bad Request
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var responseBody = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(Token);
        Assert.IsNotNull(responseBody);
        Assert.AreEqual(400, responseBody.Status);
        Assert.IsTrue(responseBody.Errors.ContainsKey("url"));
        CollectionAssert.Contains(responseBody.Errors["url"], "The Url field is required.");
    }

    [TestMethod]
    public async Task Test_PostBookmark_Ignores_CreatedAt_And_UpdatedAt()
    {
        using var fixture = new TestFixture();

        // Arrange: Prepare payload with CreatedAt and UpdatedAt set to old dates
        var oldCreatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var oldUpdatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var content = new StringContent(
            $$"""
            {
                "id": 0,
                "title": "Test Bookmark",
                "url": "https://example.com",
                "summary": "A test bookmark",
                "createdAt": "{{oldCreatedAt:o}}",
                "updatedAt": "{{oldUpdatedAt:o}}",
                "tags": ["test", "bookmark"]
            }
            """,
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await fixture.HttpClientJwt.PostAsync("/api/bookmarks", content, Token);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<Bookmark>(Token);

        // Assert: CreatedAt and UpdatedAt should be set by the API, not the payload values
        Assert.IsNotNull(created);
        Assert.AreNotEqual(oldCreatedAt, created.CreatedAt);
        Assert.AreNotEqual(oldUpdatedAt, created.UpdatedAt);
        // They should be recent (within the last minute)
        var now = DateTimeOffset.UtcNow;
        Assert.IsLessThan(1, (now - created.CreatedAt).TotalMinutes);
        Assert.IsLessThan(1, (now - created.UpdatedAt).TotalMinutes);
    }
}
