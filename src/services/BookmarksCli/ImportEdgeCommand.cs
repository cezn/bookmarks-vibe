using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class ImportEdgeCommand
{
    public static async Task HandleImportEdge(string filePath)
    {
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5002") };

        using var fs = File.OpenRead(filePath);

        if (
            await JsonSerializer.DeserializeAsync(fs, typeof(BookmarksFile), BookmarkPayloadJsonContext.Default)
            is not BookmarksFile bookmarksFile
        )
        {
            Console.WriteLine("Failed to parse bookmarks file.");
            return;
        }

        var allBookmarks = new List<BookmarkPayload>();

        void Traverse(BookmarkFolder folder, List<string> parentFolders)
        {
            if (folder == null)
                return;
            if (folder.Type == "url" && !string.IsNullOrEmpty(folder.Url))
            {
                // Check for tags in the name after the last colon, only if not followed by a space
                string title = folder.Name;
                List<string> tags = [.. parentFolders];
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
                allBookmarks.Add(
                    new BookmarkPayload
                    {
                        Title = title,
                        Url = folder.Url,
                        Tags = tags,
                    }
                );
            }
            else if (folder.Type == "folder" && folder.Children != null)
            {
                var newParents = new List<string>(parentFolders) { folder.Name };
                foreach (var child in folder.Children)
                {
                    Traverse(child, newParents);
                }
            }
        }

        // Traverse all root folders
        if (bookmarksFile.Roots?.BookmarkBar != null)
            Traverse(bookmarksFile.Roots.BookmarkBar, new List<string> { "BookmarkBar" });
        if (bookmarksFile.Roots?.Other != null)
            Traverse(bookmarksFile.Roots.Other, new List<string> { "Other" });
        if (bookmarksFile.Roots?.Synced != null)
            Traverse(bookmarksFile.Roots.Synced, new List<string> { "Synced" });

        foreach (var bm in allBookmarks)
        {
            var resp = await http.PostAsJsonAsync(
                "/api/bookmarks",
                bm,
                BookmarkPayloadJsonContext.Default.BookmarkPayload
            );
            resp.EnsureSuccessStatusCode();
            Console.WriteLine($"Imported: {bm.Title} ({bm.Url})");
        }

        Console.WriteLine($"Imported {allBookmarks.Count} bookmarks.");
    }
}

// Edge bookmarks schema
public class BookmarksFile
{
    [JsonPropertyName("checksum")]
    public required string Checksum { get; set; }

    [JsonPropertyName("roots")]
    public required Roots Roots { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }
}

public class Roots
{
    [JsonPropertyName("bookmark_bar")]
    public required BookmarkFolder BookmarkBar { get; set; }

    [JsonPropertyName("other")]
    public required BookmarkFolder Other { get; set; }

    [JsonPropertyName("synced")]
    public required BookmarkFolder Synced { get; set; }
}

public class BookmarkFolder
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; } // "folder" or "url"

    [JsonPropertyName("date_added")]
    public required string DateAdded { get; set; }

    [JsonPropertyName("date_modified")]
    public string? DateModified { get; set; }

    [JsonPropertyName("children")]
    public List<BookmarkFolder>? Children { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("guid")]
    public required string Guid { get; set; }
}
