using System.Text.Json;
using System.Text.Json.Serialization;

namespace BookmarksApi.Bookmarks;

public class EdgeBookmarksParser
{
    /// <summary>
    /// Parses a Microsoft Edge bookmarks JSON file and extracts bookmark data
    /// </summary>
    /// <param name="stream">The stream containing the Edge bookmarks JSON</param>
    /// <returns>A list of tuples containing title, URL, tags, and creation date for each bookmark</returns>
    public async Task<List<(string Title, string Url, List<string> Tags, DateTime CreatedAt)>> ParseBookmarksAsync(
        Stream stream
    )
    {
        var edgeBookmarksFile =
            await JsonSerializer.DeserializeAsync(stream, typeof(EdgeBookmarksFile)) as EdgeBookmarksFile;

        if (edgeBookmarksFile == null)
            throw new InvalidOperationException("Invalid bookmarks file format");

        var bookmarkPayloads = new List<(string Title, string Url, List<string> Tags, DateTime CreatedAt)>();

        // Traverse all root folders
        if (edgeBookmarksFile.Roots?.BookmarkBar != null)
            TraverseFolder(edgeBookmarksFile.Roots.BookmarkBar, new List<string> { "BookmarkBar" }, bookmarkPayloads);
        if (edgeBookmarksFile.Roots?.Other != null)
            TraverseFolder(edgeBookmarksFile.Roots.Other, new List<string> { "Other" }, bookmarkPayloads);
        if (edgeBookmarksFile.Roots?.Synced != null)
            TraverseFolder(edgeBookmarksFile.Roots.Synced, new List<string> { "Synced" }, bookmarkPayloads);

        return bookmarkPayloads;
    }

    private static void TraverseFolder(
        EdgeBookmarkFolder folder,
        List<string> parentFolders,
        List<(string Title, string Url, List<string> Tags, DateTime CreatedAt)> bookmarkPayloads
    )
    {
        if (folder == null)
            return;

        if (folder.Type == "url" && !string.IsNullOrEmpty(folder.Url))
        {
            // Check for tags in the name after the last colon, only if not followed by a space
            string title = folder.Name;
            var tags = new List<string>(parentFolders);

            int lastColon = folder.Name.LastIndexOf(':');
            if (lastColon != -1 && lastColon < folder.Name.Length - 1 && folder.Name[lastColon + 1] != ' ')
            {
                // Extract tags after the last colon
                string tagPart = folder.Name[(lastColon + 1)..];
                var parsedTags = tagPart
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

                if (parsedTags.Count > 0)
                {
                    tags = parsedTags;
                    title = folder.Name[..lastColon].TrimEnd();
                }
            }

            var createdAt = ConvertEdgeDateToDateTime(folder.DateAdded);
            bookmarkPayloads.Add((title, folder.Url, tags, createdAt));
        }
        else if (folder.Type == "folder" && folder.Children != null)
        {
            var newParents = new List<string>(parentFolders) { folder.Name };
            foreach (var child in folder.Children)
            {
                TraverseFolder(child, newParents, bookmarkPayloads);
            }
        }
    }

    /// <summary>
    /// Converts Microsoft Edge date format (microseconds since Unix epoch) to DateTime
    /// </summary>
    private static DateTime ConvertEdgeDateToDateTime(string edgeDateString)
    {
        if (string.IsNullOrEmpty(edgeDateString) || !long.TryParse(edgeDateString, out var microseconds))
            return DateTime.UtcNow;

        // Convert microseconds to ticks (100-nanosecond intervals)
        const long ticksPerMicrosecond = 10;
        var ticks = microseconds * ticksPerMicrosecond;
        var dateTime = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(ticks);
        return dateTime;
    }
}

/// <summary>
/// Represents the structure of Microsoft Edge bookmarks JSON export
/// </summary>
public class EdgeBookmarksFile
{
    [JsonPropertyName("checksum")]
    public required string Checksum { get; set; }

    [JsonPropertyName("roots")]
    public required EdgeRoots Roots { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }
}

public class EdgeRoots
{
    [JsonPropertyName("bookmark_bar")]
    public required EdgeBookmarkFolder BookmarkBar { get; set; }

    [JsonPropertyName("other")]
    public required EdgeBookmarkFolder Other { get; set; }

    [JsonPropertyName("synced")]
    public required EdgeBookmarkFolder Synced { get; set; }
}

public class EdgeBookmarkFolder
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; } // "folder" or "url"

    /// <summary>
    /// The date the bookmark was added, represented as a string of microseconds since the Unix epoch
    /// </summary>
    [JsonPropertyName("date_added")]
    public required string DateAdded { get; set; }

    [JsonPropertyName("date_modified")]
    public string? DateModified { get; set; }

    [JsonPropertyName("children")]
    public List<EdgeBookmarkFolder>? Children { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("guid")]
    public required string Guid { get; set; }
}
