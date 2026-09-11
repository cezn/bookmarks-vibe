namespace TagsBookmarksSubscriber;

public class BookmarksApiClient(HttpClient httpClient)
{
    public async Task AddTags(IEnumerable<string> tagNames, string userId, string? messageId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tags/bulk/add")
        {
            Content = JsonContent.Create(new { TagNames = tagNames }),
        };
        request.Headers.Add("X-Idempotency-Key", messageId);
        request.Headers.Add("X-UserId", userId);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveTags(IEnumerable<string> tagNames, string userId, string? messageId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tags/bulk/remove")
        {
            Content = JsonContent.Create(new { TagNames = tagNames }),
        };
        request.Headers.Add("X-Idempotency-Key", messageId);
        request.Headers.Add("X-UserId", userId);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
