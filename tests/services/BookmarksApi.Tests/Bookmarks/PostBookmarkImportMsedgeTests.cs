using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookmarksApi.Bookmarks;
using BookmarksApi.Outbox;
using Microsoft.AspNetCore.Http;

namespace BookmarksApi.Tests.Bookmarks;

[TestClass]
public sealed class PostBookmarkImportMsedgeTests
{
    public TestContext TestContext { get; set; }
    public CancellationToken Token => TestContext.CancellationToken;

    [TestMethod]
    public async Task Test_ImportMsEdgeBookmarks_With_Valid_Json_File()
    {
        using var fixture = new TestFixture();

        // Arrange: Create a valid MS Edge bookmarks JSON file
        var jsonContent = """
            {
              "checksum": "abc123",
              "version": 1,
              "roots": {
                "bookmark_bar": {
                  "id": "1",
                  "name": "Bookmarks bar",
                  "type": "folder",
                  "date_added": "1609459200",
                  "guid": "00000000-0000-4000-a000-000000000001",
                  "children": [
                    {
                      "id": "2",
                      "name": "GitHub",
                      "type": "url",
                      "url": "https://github.com",
                      "date_added": "1609459200",
                      "guid": "00000000-0000-4000-a000-000000000002"
                    },
                    {
                      "id": "3",
                      "name": "Stack Overflow",
                      "type": "url",
                      "url": "https://stackoverflow.com",
                      "date_added": "1609459200",
                      "guid": "00000000-0000-4000-a000-000000000003"
                    },
                    {
                      "id": "4",
                      "name": "Google:search,tools",
                      "type": "url",
                      "url": "https://www.google.com",
                      "date_added": "1609459200",
                      "guid": "00000000-0000-4000-a000-000000000004"
                    }
                  ]
                },
                "other": {
                  "id": "5",
                  "name": "Other",
                  "type": "folder",
                  "date_added": "1609459200",
                  "guid": "00000000-0000-4000-a000-000000000005"
                },
                "synced": {
                  "id": "6",
                  "name": "Synced",
                  "type": "folder",
                  "date_added": "1609459200",
                  "guid": "00000000-0000-4000-a000-000000000006"
                }
              }
            }
            """;

        using var form = new MultipartFormDataContent();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        form.Add(fileContent, "file", "bookmarks.json");

        // Act
        var response = await fixture.HttpClientJwt.PostAsync("/api/bookmarks/import/msedge", form, Token);

        // Assert
        response.EnsureSuccessStatusCode();
        var imported = await response.Content.ReadFromJsonAsync<List<Bookmark>>(Token);
        Assert.IsNotNull(imported);
        Assert.HasCount(3, imported);

        // Verify imported bookmarks are in the database
        var dbBookmarks = await fixture.Connection.SearchBookmarksCursorAsync(
            userId: fixture.UserId.ToString(),
            q: "",
            ct: Token
        );
        Assert.Contains(b => b.Title == "GitHub", dbBookmarks);
        Assert.Contains(b => b.Title == "Stack Overflow", dbBookmarks);
        Assert.Contains(b => b.Title == "Google", dbBookmarks);
    }

    [TestMethod]
    public async Task Test_ImportMsEdgeBookmarks_With_No_File()
    {
        using var fixture = new TestFixture();

        // Arrange
        using var form = new MultipartFormDataContent();

        // Act
        var response = await fixture.HttpClientJwt.PostAsync("/api/bookmarks/import/msedge", form, Token);

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
        // TODO: verify that response matches something like:
        // {"type":"https://tools.ietf.org/html/rfc9110#section-15.6.1","title":"An error occurred while processing your request.","status":500,"traceId":"00-c29ad4530eb1f9a7d53c943d0b059b09-17b7a03e6348c4c2-01"}
        // var responseBody = await response.Content.ReadAsStringAsync();
    }

    [TestMethod]
    public async Task Test_ImportMsEdgeBookmarks_With_Empty_File()
    {
        using var fixture = new TestFixture();

        // Arrange
        var emptyContent = "";
        using var form = new MultipartFormDataContent();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(emptyContent));
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        form.Add(fileContent, "file", "bookmarks.json");

        // Act
        var response = await fixture.HttpClientJwt.PostAsync("/api/bookmarks/import/msedge", form, Token);

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Test_ImportMsEdgeBookmarks_Creates_Outbox_Messages()
    {
        using var fixture = new TestFixture();

        // Arrange: Create a valid MS Edge bookmarks JSON file
        var jsonContent = """
            {
              "checksum": "abc123",
              "version": 1,
              "roots": {
                "bookmark_bar": {
                  "id": "1",
                  "name": "Bookmarks bar",
                  "type": "folder",
                  "date_added": "1609459200",
                  "guid": "00000000-0000-4000-a000-000000000001",
                  "children": [
                    {
                      "id": "2",
                      "name": "Example",
                      "type": "url",
                      "url": "https://example.com",
                      "date_added": "1609459200",
                      "guid": "00000000-0000-4000-a000-000000000002"
                    }
                  ]
                },
                "other": {
                  "id": "5",
                  "name": "Other",
                  "type": "folder",
                  "date_added": "1609459200",
                  "guid": "00000000-0000-4000-a000-000000000005"
                },
                "synced": {
                  "id": "6",
                  "name": "Synced",
                  "type": "folder",
                  "date_added": "1609459200",
                  "guid": "00000000-0000-4000-a000-000000000006"
                }
              }
            }
            """;

        using var form = new MultipartFormDataContent();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        form.Add(fileContent, "file", "bookmarks.json");

        // Act
        var response = await fixture.HttpClientJwt.PostAsync("/api/bookmarks/import/msedge", form, Token);
        response.EnsureSuccessStatusCode();
        var imported = await response.Content.ReadFromJsonAsync<List<Bookmark>>(Token);

        // Assert: Verify outbox messages were created for each imported bookmark
        Assert.IsNotNull(imported);
        Assert.HasCount(1, imported);

        var bookmarkId = imported[0].Id;
        var outboxMessages = await fixture.Connection.GetOutboxMessagesByAggregateIdAsync(bookmarkId.ToString(), Token);
        Assert.IsNotNull(outboxMessages);
        Assert.AreEqual(1, outboxMessages.Count());
        Assert.AreEqual("bookmark_created", outboxMessages.First().Type);
    }

    [TestMethod]
    public async Task Test_ImportMsEdgeBookmarks_Returns_Imported_Bookmarks()
    {
        using var fixture = new TestFixture();

        // Arrange
        var jsonContent = """
            {
              "checksum": "abc123",
              "version": 1,
              "roots": {
                "bookmark_bar": {
                  "id": "1",
                  "name": "Bookmarks bar",
                  "type": "folder",
                  "date_added": "1609459200",
                  "guid": "00000000-0000-4000-a000-000000000001",
                  "children": [
                    {
                      "id": "2",
                      "name": "Test One",
                      "type": "url",
                      "url": "https://test1.com",
                      "date_added": "1609459200",
                      "guid": "00000000-0000-4000-a000-000000000002"
                    },
                    {
                      "id": "3",
                      "name": "Test Two",
                      "type": "url",
                      "url": "https://test2.com",
                      "date_added": "1609459200",
                      "guid": "00000000-0000-4000-a000-000000000003"
                    }
                  ]
                },
                "other": {
                  "id": "5",
                  "name": "Other",
                  "type": "folder",
                  "date_added": "1609459200",
                  "guid": "00000000-0000-4000-a000-000000000005"
                },
                "synced": {
                  "id": "6",
                  "name": "Synced",
                  "type": "folder",
                  "date_added": "1609459200",
                  "guid": "00000000-0000-4000-a000-000000000006"
                }
              }
            }
            """;

        using var form = new MultipartFormDataContent();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        form.Add(fileContent, "file", "bookmarks.json");

        // Act
        var response = await fixture.HttpClientJwt.PostAsync("/api/bookmarks/import/msedge", form, Token);
        response.EnsureSuccessStatusCode();
        var imported = await response.Content.ReadFromJsonAsync<List<Bookmark>>(Token);

        // Assert: Verify response structure
        Assert.IsNotNull(imported);
        Assert.HasCount(2, imported);

        // Verify all bookmarks have required fields set
        foreach (var bookmark in imported)
        {
            Assert.AreNotEqual(-1, bookmark.Id);
            Assert.IsNotNull(bookmark.Title);
            Assert.IsNotNull(bookmark.Url);
            Assert.IsGreaterThan(DateTimeOffset.MinValue, bookmark.CreatedAt);
            Assert.IsGreaterThan(DateTimeOffset.MinValue, bookmark.UpdatedAt);
        }
    }
}
