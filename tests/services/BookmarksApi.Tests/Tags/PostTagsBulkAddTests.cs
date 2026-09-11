using System.Net;
using System.Net.Http.Json;
using BookmarksApi.Tags;
using Microsoft.Extensions.Logging;

namespace BookmarksApi.Tests.Tags;

[TestClass]
public sealed class PostTagsBulkAddTests
{
    public TestContext TestContext { get; set; }
    public CancellationToken Token => TestContext.CancellationToken;

    [TestMethod]
    public async Task Test_BulkAddTests_With_Single_Tag()
    {
        using var fixture = new TestFixture();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tags/bulk/add")
        {
            Content = JsonContent.Create(new { TagNames = new[] { "t1" } }),
            Headers = { { "X-UserId", fixture.UserId.ToString() } },
        };
        var response = await fixture.HttpClientApiKey.SendAsync(request, Token);

        fixture.Logger.LogInformation("Assert:");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var createdTags = await response.Content.ReadFromJsonAsync<Tag[]>(Token);
        Assert.IsNotNull(createdTags);
        Assert.HasCount(1, createdTags);
        Assert.AreEqual("t1", createdTags[0].Name);
        Assert.AreEqual(1, createdTags[0].UsageCount);
        Assert.AreEqual(fixture.UserId.ToString(), createdTags[0].UserId);

        var dbTags = (await fixture.Connection.GetAllTagsAsync(userId: fixture.UserId.ToString(), Token)).ToArray();
        Assert.HasCount(1, dbTags);
        Assert.AreEqual("t1", dbTags[0].Name);
        Assert.AreEqual(1, dbTags[0].UsageCount);
    }

    [TestMethod]
    public async Task Test_BulkAddTests_With_Multiple_Tags()
    {
        using var fixture = new TestFixture();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tags/bulk/add")
        {
            Content = JsonContent.Create(new { TagNames = new[] { "t1", "t2", "t3" } }),
            Headers = { { "X-UserId", fixture.UserId.ToString() } },
        };
        var response = await fixture.HttpClientApiKey.SendAsync(request, Token);

        // Assert:
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var createdTags = (await response.Content.ReadFromJsonAsync<Tag[]>(Token))!.OrderBy(x => x.Name).ToArray();
        Assert.IsNotNull(createdTags);
        Assert.HasCount(3, createdTags);
        Assert.AreEqual("t1", createdTags[0].Name);
        Assert.AreEqual(1, createdTags[0].UsageCount);
        Assert.AreEqual(fixture.UserId.ToString(), createdTags[0].UserId);
        Assert.AreEqual("t2", createdTags[1].Name);
        Assert.AreEqual(1, createdTags[1].UsageCount);
        Assert.AreEqual(fixture.UserId.ToString(), createdTags[1].UserId);
        Assert.AreEqual("t3", createdTags[2].Name);
        Assert.AreEqual(1, createdTags[2].UsageCount);
        Assert.AreEqual(fixture.UserId.ToString(), createdTags[2].UserId);

        var dbTags = (await fixture.Connection.GetAllTagsAsync(userId: fixture.UserId.ToString(), Token))
            .OrderBy(x => x.Name)
            .ToArray();
        Assert.HasCount(3, dbTags);
        Assert.AreEqual("t1", dbTags[0].Name);
        Assert.AreEqual(1, dbTags[0].UsageCount);
        Assert.AreEqual("t2", dbTags[1].Name);
        Assert.AreEqual(1, dbTags[1].UsageCount);
        Assert.AreEqual("t3", dbTags[2].Name);
        Assert.AreEqual(1, dbTags[2].UsageCount);
    }

    [TestMethod]
    public async Task Test_BulkAddTests_Idempotence()
    {
        using var fixture = new TestFixture();

        var httpMessage = () =>
            new HttpRequestMessage(HttpMethod.Post, "/api/tags/bulk/add")
            {
                Headers =
                {
                    { "X-Idempotency-Key", "5080dd2a-e7c8-5cdf-86b2-51597b9284d9" },
                    { "X-UserId", fixture.UserId.ToString() },
                },
                Content = JsonContent.Create(new { TagNames = new[] { "t1" } }),
            };

        fixture.Logger.LogInformation("Creating tag");
        var response = await fixture.HttpClientApiKey.SendAsync(httpMessage(), Token);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("false", response.Headers.GetValues("X-Idempotency-Hit").FirstOrDefault());

        fixture.Logger.LogInformation("Creating tag retry");
        var responseRetry = await fixture.HttpClientApiKey.SendAsync(httpMessage(), Token);
        Assert.AreEqual(HttpStatusCode.OK, responseRetry.StatusCode);
        Assert.AreEqual("true", responseRetry.Headers.GetValues("X-Idempotency-Hit").FirstOrDefault());

        fixture.Logger.LogInformation("Assert:");
        var createdTags = await responseRetry.Content.ReadFromJsonAsync<Tag[]>(Token);
        Assert.IsNotNull(createdTags);
        Assert.HasCount(1, createdTags);
        Assert.AreEqual("t1", createdTags[0].Name);
        Assert.AreEqual(1, createdTags[0].UsageCount);
        Assert.AreEqual(fixture.UserId.ToString(), createdTags[0].UserId);

        var dbTags = (await fixture.Connection.GetAllTagsAsync(userId: fixture.UserId.ToString(), Token)).ToArray();
        Assert.HasCount(1, dbTags);
        Assert.AreEqual("t1", dbTags[0].Name);
        Assert.AreEqual(1, dbTags[0].UsageCount);
    }

    [TestMethod]
    public async Task Test_BulkAddTests_With_Duplicate_Tag_In_Separate_Calls_Increments_Count()
    {
        using var fixture = new TestFixture();

        // First call: add tag "t1"
        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/tags/bulk/add")
        {
            Content = JsonContent.Create(new { TagNames = new[] { "t1" } }),
            Headers = { { "X-UserId", fixture.UserId.ToString() } },
        };
        var response1 = await fixture.HttpClientApiKey.SendAsync(request1, Token);

        Assert.AreEqual(HttpStatusCode.OK, response1.StatusCode);
        var createdTags1 = await response1.Content.ReadFromJsonAsync<Tag[]>(Token);
        Assert.IsNotNull(createdTags1);
        Assert.HasCount(1, createdTags1);
        Assert.AreEqual("t1", createdTags1[0].Name);
        Assert.AreEqual(1, createdTags1[0].UsageCount);
        Assert.AreEqual(fixture.UserId.ToString(), createdTags1[0].UserId);

        // Second call: add the same tag "t1" again
        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/tags/bulk/add")
        {
            Content = JsonContent.Create(new { TagNames = new[] { "t1" } }),
            Headers = { { "X-UserId", fixture.UserId.ToString() } },
        };
        var response2 = await fixture.HttpClientApiKey.SendAsync(request2, Token);

        Assert.AreEqual(HttpStatusCode.OK, response2.StatusCode);
        var createdTags2 = await response2.Content.ReadFromJsonAsync<Tag[]>(Token);
        Assert.IsNotNull(createdTags2);
        Assert.HasCount(1, createdTags2);
        Assert.AreEqual("t1", createdTags2[0].Name);
        Assert.AreEqual(2, createdTags2[0].UsageCount);
        Assert.AreEqual(fixture.UserId.ToString(), createdTags2[0].UserId);

        // Verify in DB
        var dbTags = (await fixture.Connection.GetAllTagsAsync(userId: fixture.UserId.ToString(), Token)).ToArray();
        Assert.HasCount(1, dbTags);
        Assert.AreEqual("t1", dbTags[0].Name);
        Assert.AreEqual(2, dbTags[0].UsageCount);
    }

    [TestMethod]
    public async Task Test_UserId_Validation()
    {
        using var fixture = new TestFixture();
        fixture.Logger.LogInformation("Act:");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tags/bulk/add")
        {
            Content = JsonContent.Create(new { TagNames = new[] { "t1" } }),
            Headers = { { "X-UserId", "" } },
        };
        var response1 = await fixture.HttpClientApiKey.SendAsync(request, Token);

        fixture.Logger.LogInformation("Assert:");
        // TODO: find out how to return 400 when userId is not found
        Assert.AreEqual(HttpStatusCode.InternalServerError, response1.StatusCode);
        // Assert.AreEqual(HttpStatusCode.BadRequest, response1.StatusCode);
        Assert.AreEqual(
            0,
            (await fixture.Connection.GetAllTagsAsync(userId: fixture.UserId.ToString(), Token)).Count()
        );
        Assert.AreEqual(0, (await fixture.Connection.GetAllTagsAsync(userId: "", Token)).Count());
        Assert.AreEqual(0, (await fixture.Connection.GetAllTagsAsync(userId: null!, Token)).Count());
        // var errors = await response1.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        // Assert.AreEqual("The UserId field is required.", errors!.Errors["userId"].Single());
    }
}
