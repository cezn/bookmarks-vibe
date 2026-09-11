using Microsoft.Extensions.AI;

namespace SummarizeApi;

public record SummarizeResult(string Summary, string[] Tags);

public class Summarizer(IChatClient chatClient, ILogger<Summarizer> logger)
{
    public async Task<bool> Ping(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await chatClient.GetResponseAsync("ping", cancellationToken: cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<SummarizeResult> Summarize(
        PageInfo pageInfo,
        string[] suggestedTags,
        CancellationToken cancellationToken = default
    )
    {
        string suggestedTagsPrompt = "";
        if (suggestedTags.Any())
        {
            suggestedTagsPrompt = $"""
                If any of these suggested tags are highly related, prefer them: {string.Join(
                        ", ",
                        suggestedTags
                    )}.
                """;
        }

        List<AIContent> aIContents = pageInfo switch
        {
            YoutubePageInfo youtubePageInfo =>
            [
                new TextContent(youtubePageInfo.Transcript),
                new TextContent(
                    $"""
                    Summarize this transcript of YouTube video titled '{youtubePageInfo.Title}'. Extract the most relevant tags based on the content.
                    {suggestedTagsPrompt}
                    Only include tags that are strongly connected to the video.
                    """
                ),
                new TextContent(youtubePageInfo.Transcript),
            ],
            _ =>
            [
                new TextContent(pageInfo.TextContent),
                new TextContent(
                    $"""
                    Summarize this document. Extract the most relevant tags based on the content.
                    {suggestedTagsPrompt}
                    Only include tags that are strongly connected to the document.
                    """
                ),
            ],
        };

        ChatResponse<SummarizeResult> res;
        try
        {
            res = await chatClient.GetResponseAsync<SummarizeResult>(
                new ChatMessage(role: ChatRole.User, contents: aIContents),
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during LLM call");
            return new SummarizeResult("", Array.Empty<string>());
        }

        if (res.TryGetResult(out var summaryResult))
            return summaryResult;
        else
        {
            logger.LogError("Failed to parse LLM response");
            return new SummarizeResult("", Array.Empty<string>());
        }
    }
}
