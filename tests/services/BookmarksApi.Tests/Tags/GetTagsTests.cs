using System.Net.Http.Json;
using System.Threading;
using BookmarksApi.Tags;
using static BookmarksApi.Tags.TagsEndpoints;

namespace BookmarksApi.Tests.Tags;

[TestClass]
public sealed class GetTagsTests
{
    public TestContext TestContext { get; set; }
    public CancellationToken Token => TestContext.CancellationToken;

    [TestMethod]
    public async Task Test_GetTags()
    {
        using var fixture = new TestFixture();

        // Arrange
        var tag = fixture.NewTagBuilder().Build();
        await fixture.Connection.AddTagAsync(tag, Token);

        // Act
        var response = await fixture.HttpClientJwt.GetFromJsonAsync<GetTagsResponse>(
            $"/api/tags?q={Uri.EscapeDataString(tag.Name)}&sort=name&limit=10",
            Token
        );

        // Assert
        Assert.IsNotNull(response);
        var tags = response.Tags;
        var found = tags.FirstOrDefault(t => t.Name == tag.Name);
        Assert.IsNotNull(found);
        Assert.AreEqual(tag.Name, found.Name);
        Assert.AreEqual(tag.UsageCount, found.UsageCount);
        Assert.AreEqual(tag.CreatedAt, found.CreatedAt);
    }

    [TestMethod]
    [DataRow("id")]
    [DataRow("name")]
    [DataRow("usageCount")]
    [DataRow("createdAt")]
    public async Task Test_GetTags_CursorPagination(string sortField)
    {
        using var fixture = new TestFixture();

        // Arrange: Insert 3 tags with unique names for pagination
        var guidPrefix = Guid.NewGuid().ToString();
        var names = new[] { "CursorTag1", "CursorTag2", "CursorTag3" }.Select(n => $"{guidPrefix}_{n}").ToArray();
        var tags = names
            .Select(
                (name, i) =>
                    fixture
                        .NewTagBuilder()
                        .WithName(name)
                        .WithUsageCount(i + 1)
                        .WithCreatedAt(DateTimeOffset.UtcNow.AddMinutes(i))
                        .Build()
            )
            .ToList();

        foreach (var tag in tags)
            await fixture.Connection.AddTagAsync(tag, Token);

        // Act: Get first 2 tags
        var response1 = await fixture.HttpClientJwt.GetFromJsonAsync<GetTagsResponse>(
            $"/api/tags?q={Uri.EscapeDataString(guidPrefix)}&sort={sortField}&limit=2&sortDirection=ASC",
            Token
        );
        Assert.IsNotNull(response1);
        Assert.HasCount(2, response1.Tags);

        // Use nextCursor to get the next page
        var nextCursor = response1.NextCursor;
        Assert.IsNotNull(nextCursor);

        var response2 = await fixture.HttpClientJwt.GetFromJsonAsync<GetTagsResponse>(
            $"/api/tags?q={Uri.EscapeDataString(guidPrefix)}&sort={sortField}&limit=2&sortDirection=ASC&cursor={Uri.EscapeDataString(nextCursor)}",
            Token
        );
        Assert.IsNotNull(response2);
        Assert.HasCount(1, response2.Tags);

        // Assert: All tags are returned in order, no duplicates
        var allNames = response1.Tags.Concat(response2.Tags).Select(t => t.Name).ToList();
        Assert.AreSequenceEqual(names, allNames, SequenceOrder.InAnyOrder);

        // Assert: CreatedAt is returned and matches for all tags
        var allTags = response1.Tags.Concat(response2.Tags).ToList();
        foreach (var tag in tags)
        {
            var found = allTags.FirstOrDefault(t => t.Name == tag.Name);
            Assert.IsNotNull(found);
            // Compare CreatedAt up to seconds precision to avoid tick mismatches
            Assert.AreEqual(tag.CreatedAt, found.CreatedAt);
        }
    }

    [TestMethod]
    public async Task Test_GetTags_WithoutQueryParameters_ReturnsOk()
    {
        using var fixture = new TestFixture();

        // Act
        var response = await fixture.HttpClientJwt.GetAsync("/api/tags", Token);

        // Assert
        Assert.IsTrue(response.IsSuccessStatusCode, $"Expected OK, got {response.StatusCode}");
    }
}
