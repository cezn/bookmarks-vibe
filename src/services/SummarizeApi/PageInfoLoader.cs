using System.Diagnostics;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Hybrid;
using ServiceDefaults;

namespace SummarizeApi;

public record PageInfo(string? Title, string? TextContent);

public record YoutubePageInfo(string? Title, string? TextContent, string? Transcript) : PageInfo(Title, TextContent);

public class PageInfoLoader(ILogger<PageInfoLoader> logger, HttpClient httpClient, HybridCache cache)
{
    public async ValueTask<PageInfo> GetPageInfo(string url, CancellationToken cancellationToken)
    {
        using var activity = Monitoring.ActivitySource.StartActivity("GetPageInfo", ActivityKind.Internal);
        logger.LogInformation("Fetching page info for URL: {Url}", url);

        try
        {
            return await cache.GetOrCreateAsync(
                key: $"PageInfo:{url}",
                state: (activity, url),
                factory: async (state, cancel) =>
                {
                    // Workaround, see https://github.com/dotnet/extensions/issues/6543
                    Activity.Current = state.activity;
                    PageInfo pageInfo = await GetPageInfoAsync(state.url, cancel);
                    Activity.Current?.SetTag("PageInfoType", pageInfo.GetType().Name);

                    return pageInfo;
                },
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }

    private async Task<PageInfo> GetPageInfoAsync(string url, CancellationToken ct)
    {
        var html = await httpClient.GetStringAsync(url, ct);
        var pageInfo = ExtractMainContent(html);
        if (IsYouTubeUrl(url, out var videoId) && !string.IsNullOrEmpty(videoId))
        {
            // // for now, just use 'Title' for youtube
            // logger.LogInformation("Detected YouTube URL, fetching transcript for video ID: {VideoId}", videoId);
            // var transcript = await GetYouTubeTranscriptAsync(videoId, ct);
            // logger.LogInformation(
            //     "Fetched YouTube transcript for video ID: {VideoId}, transcript length: {TranscriptLength}",
            //     videoId,
            //     transcript?.Length ?? 0
            // );
            // return new YoutubePageInfo(pageInfo.Title, pageInfo.TextContent, transcript);

            return new YoutubePageInfo(pageInfo.Title, "", "");
        }

        return pageInfo;
    }

    static PageInfo ExtractMainContent(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var mainNode =
            doc.DocumentNode.SelectSingleNode("//main//article")
            ?? doc.DocumentNode.SelectSingleNode("//main")
            ?? doc.DocumentNode.SelectSingleNode("//article")
            ?? doc.DocumentNode.SelectSingleNode("//body");
        var textContent = mainNode?.InnerText?.Trim();
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        var title = titleNode?.InnerText?.Trim();

        return new(title, textContent);
    }

    // Helper to detect YouTube URLs and extract video ID
    static bool IsYouTubeUrl(string url, out string? videoId)
    {
        videoId = null;
        try
        {
            var uri = new Uri(url);
            if (uri.Host.EndsWith("youtube.com", StringComparison.OrdinalIgnoreCase))
            {
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                videoId = query["v"];
                return !string.IsNullOrEmpty(videoId);
            }
            if (uri.Host.EndsWith("youtu.be", StringComparison.OrdinalIgnoreCase))
            {
                videoId = uri.AbsolutePath.TrimStart('/');
                return !string.IsNullOrEmpty(videoId);
            }
        }
        catch { }
        return false;
    }

    // Fetch YouTube transcript using a public API or fallback to empty string
    async Task<string?> GetYouTubeTranscriptAsync(string videoId, CancellationToken cancellationToken)
    {
        // This uses a public unofficial API for demonstration.
        // In production, use a proper YouTube Data API or a library.
        try
        {
            var url = $"https://youtubetranscript.com/?server=1&video_id={videoId}";
            var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            // Parse transcript from HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var transcriptNode = doc.DocumentNode.SelectSingleNode("//div[@id='transcript']");
            if (transcriptNode == null)
                return null;
            var transcript = transcriptNode.InnerText?.Trim();
            return transcript;
        }
        catch
        {
            return null;
        }
    }
}
