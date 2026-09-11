using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookmarksApi.Setup;

internal class JsonExceptionHandler(ILogger<JsonExceptionHandler> logger, IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is not BadHttpRequestException { InnerException: JsonException } _)
            return false;

        logger.LogDebug("Handling JsonException");
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid JSON",
            Detail = "The JSON payload is in an incorrect format.",
        };

        await problemDetails.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = details,
                Exception = exception,
            }
        );

        return true;
    }
}
