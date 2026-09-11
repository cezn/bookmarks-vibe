using System.Diagnostics;
using ServiceDefaults;

namespace SummarizeApi;

public class BookmarksApiClient(HttpClient httpClient)
{
    public async Task<HashSet<string>> GetAllTagNamesAsync(string[] initialTags, CancellationToken cancellationToken)
    {
        var tagNames = new HashSet<string>(initialTags ?? [], comparer: StringComparer.InvariantCultureIgnoreCase);
        string? cursor = null;
        do
        {
            var url = $"/api/tags?limit=100" + (cursor != null ? $"&cursor={Uri.EscapeDataString(cursor)}" : "");
            var tagsResponse = await httpClient.GetAsync(url, cancellationToken);
            if (!tagsResponse.IsSuccessStatusCode)
                break;
            var tagsResult = await tagsResponse.Content.ReadFromJsonAsync<GetTagsResponse>(
                cancellationToken: cancellationToken
            );
            if (tagsResult is not { Tags.Count: > 0 })
                break;
            foreach (var tag in tagsResult.Tags)
                tagNames.Add(tag.Name);
            cursor = tagsResult.NextCursor;
        } while (!string.IsNullOrEmpty(cursor));
        return tagNames;
    }

    public async Task<string[]> GetSuggestedTags(string[] requestTags, CancellationToken cancellationToken)
    {
        using var activity = Monitoring.ActivitySource.StartActivity("GetSuggestedTags", ActivityKind.Internal);
        string[] allSuggestedTags = requestTags ?? [];
        activity?.SetTag("SuggestedTags.Count", allSuggestedTags.Length);
        try
        {
            allSuggestedTags = [.. await GetAllTagNamesAsync(allSuggestedTags, cancellationToken)];
            activity?.SetTag("AllSuggestedTags.Count", allSuggestedTags.Length);
        }
        catch (Exception ex)
        {
            // Ignore errors, fallback to only requestTags
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
        }

        return allSuggestedTags;
    }
}

// DTOs for deserialization
public record Tag(int Id, string Name, int UsageCount);

public record GetTagsResponse(List<Tag> Tags, string? NextCursor);
