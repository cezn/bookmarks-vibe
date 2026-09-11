using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using SummarizeApi.Extensions;

namespace SummarizeApi;

public static class SummarizerEndpoints
{
    public static void MapSummarizerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/summarize").RequireCors();
        group.MapPost("", Summarize);
    }

    record SummarizeRequest(string Url, string[] SuggestedTags);

    record SummarizeResponse(string Title, string? Summary, string[] Tags);

    /// <summary>
    /// Summarize a webpage and extract metadata
    /// </summary>
    /// <param name="request">The request containing the URL and suggested tags to process</param>
    /// <response code="200">Successfully summarized the webpage</response>
    /// <response code="400">Invalid or local/private URL provided</response>
    /// <response code="500">Internal server error</response>
    [ProducesResponseType(typeof(SummarizeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private static async Task<IResult> Summarize(
        SummarizeRequest request,
        Summarizer summarizer,
        BookmarksApiClient bookmarksApi,
        PageInfoLoader pageInfoLoader,
        [FromKeyedServices("OllamaHttpClient")] CircuitBreakerStateProvider ollamaState,
        ILoggerFactory loggerFactory,
        CancellationToken ct
    )
    {
        var logger = loggerFactory.CreateLogger(nameof(SummarizerEndpoints));

        if (!await Utils.IsValidExternalUrl(request.Url))
        {
            logger.LogWarning("Invalid or local/private URL provided: {Url}", request.Url);
            return Results.BadRequest("Invalid or local/private URL.");
        }

        var ollamaOk = ollamaState.CircuitState == CircuitState.Closed;
        if (!ollamaOk) // reevaluate circuit braker
            ollamaOk = await summarizer.Ping(ct);

        if (!ollamaOk)
            logger.LogInformation(
                "Ollama service is currently unavailable due to circuit breaker 'Open' state. Summarization is degraded."
            );

        var (suggestedTags, pageInfo) = await Task.WhenAll(
            t1: ollamaOk
                ? bookmarksApi.GetSuggestedTags(request.SuggestedTags, ct)
                : Task.FromResult(Array.Empty<string>()),
            t2: pageInfoLoader.GetPageInfo(request.Url, ct).AsTask()
        );

        var (summary, tags) = ollamaOk
            ? await summarizer.Summarize(pageInfo, suggestedTags, ct)
            : new SummarizeResult("", []);

        return Results.Ok(new SummarizeResponse(Title: pageInfo.Title!, Summary: summary, Tags: tags));
    }
}
