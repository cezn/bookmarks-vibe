using System.Net.Http.Json;

public static class SeedCommand
{
    public static async Task HandleSeed(string apikey)
    {
        BookmarkPayload[] bookmarks =
        [
            new BookmarkPayload
            {
                Title = "PostgreSQL Official Docs",
                Url = "https://www.postgresql.org/docs/",
                Tags = ["Databases"],
            },
            new BookmarkPayload
            {
                Title = "MDN Web Docs",
                Url = "https://developer.mozilla.org/",
                Tags = ["Web Development", "Programming"],
            },
            new BookmarkPayload
            {
                Title = "GitHub",
                Url = "https://github.com/",
                Tags = ["Programming", "Productivity"],
            },
            new BookmarkPayload
            {
                Title = "OpenAI",
                Url = "https://openai.com/",
                Tags = ["AI", "Programming"],
            },
            new BookmarkPayload
            {
                Title = "Todoist",
                Url = "https://todoist.com/",
                Tags = ["Productivity"],
            },
        ];

        var http = new HttpClient { BaseAddress = new Uri("http://localhost:5002") };
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            apikey
        );

        foreach (var bm in bookmarks)
        {
            var payload = new BookmarkPayload
            {
                Title = bm.Title,
                Url = bm.Url,
                Tags = bm.Tags,
            };
            var resp = await http.PostAsJsonAsync(
                "/api/bookmarks",
                payload,
                BookmarkPayloadJsonContext.Default.BookmarkPayload
            );
            resp.EnsureSuccessStatusCode();
        }
    }
}
