using System.Diagnostics;
using BookmarksApi.Shared;
using Microsoft.AspNetCore.Authentication;
using Npgsql;

namespace BookmarksApi.Idempotency;

public class IdempotencyMiddleware(
    RequestDelegate next,
    ILogger<IdempotencyMiddleware> logger,
    UserIdProvider userIdProvider
)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<IdempotencyMiddleware> _logger = logger;
    private readonly UserIdProvider _userIdProvider = userIdProvider;

    public async Task InvokeAsync(HttpContext context)
    {
        var ct = context.RequestAborted;
        var operationName = GetOperationName(context);
        var con = context.RequestServices.GetRequiredKeyedService<NpgsqlConnection>("rw");
        await con.OpenTraceAsync(ct);
        using var tran = await con.BeginTransactionAsync();

        var hasIdempotencyMetadata = context.GetEndpoint()?.Metadata.GetMetadata<IdempotentAttribute>() != null;
        if (!hasIdempotencyMetadata)
        {
            await _next(context);
            await tran.CommitAsync(ct);
            return;
        }

        var idempotencyKey = context.Request.Headers.TryGetValue("X-Idempotency-Key", out var keyValue)
            ? keyValue.ToString()
            : null;

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _next(context);
            await tran.CommitAsync(ct);
            return;
        }

        var userId = _userIdProvider.FindUserId(context);
        if (userId == null)
        {
            _logger.LogWarning("Could not determine UserId for idempotency check");
            await _next(context);
            await tran.CommitAsync(ct);
            return;
        }

        var checkResult = await con.GetOrInsertIdempotencyKeyAsync(idempotencyKey, userId, operationName);
        if (checkResult.ExistingResponse != null)
        {
            context.Response.Headers.Append("X-Idempotency-Hit", "true");
            Activity.Current?.AddTag("idempotency.hit", true);

            await tran.CommitAsync(ct);
            await WriteResponseFromCache(context, checkResult.ExistingResponse);
            return;
        }

        Activity.Current?.AddTag("idempotency.hit", false);
        context.Response.Headers.Append("X-Idempotency-Hit", "false");

        if (checkResult.IsInProgress)
        {
            await tran.CommitAsync();
            context.Response.StatusCode = 499;
            return;
        }

        // Capture the response body by replacing the response stream
        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);

            // Extract response details
            var statusCode = context.Response.StatusCode;
            if (statusCode >= 400)
                return;

            var headers = ExtractHeaders(context);
            var body = memoryStream.ToArray();

            // Store the response in the database
            await con.StoreIdempotencyResponseAsync(idempotencyKey, userId, operationName, statusCode, headers, body);

            await tran.CommitAsync();

            // Write the captured response back to the original stream
            if (statusCode != 204)
                await originalBodyStream.WriteAsync(body);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static Dictionary<string, string> ExtractHeaders(HttpContext context)
    {
        var headers = new Dictionary<string, string>();
        foreach (var header in context.Response.Headers)
        {
            // Skip content-length and transfer-encoding headers that will be set automatically
            if (
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)
                || header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
            )
                continue;

            headers[header.Key] = header.Value.ToString();
        }
        return headers;
    }

    private static async Task WriteResponseFromCache(
        HttpContext context,
        IdempotencyDbConnectionExtensions.IdempotencyResponse response
    )
    {
        context.Response.StatusCode = response.StatusCode;

        // Set headers from cache
        foreach (var header in response.Headers)
        {
            if (
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)
                || header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
            )
                continue;

            context.Response.Headers.Append(header.Key, header.Value);
        }

        // Write body
        if (response.StatusCode != 204)
            await context.Response.Body.WriteAsync(response.Body);
    }

    private static string GetOperationName(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        return endpoint?.DisplayName ?? $"{context.Request.Method} {context.Request.Path}";
    }
}

public class UserIdProvider(ILogger<UserIdProvider> logger)
{
    private readonly ILogger<UserIdProvider> _logger = logger;

    public string? FindUserId(HttpContext context)
    {
        var scheme = context
            .Features.Get<IAuthenticateResultFeature>()
            ?.AuthenticateResult?.Ticket?.AuthenticationScheme;

        if (string.IsNullOrWhiteSpace(scheme))
        {
            _logger.LogWarning("Authentication scheme not found in features");
            return null;
        }

        _logger.LogInformation("Finding user with scheme: {Scheme}", scheme);

        if (scheme == "ApiKey")
            return FindUserIdFromApiKey(context);

        if (scheme == "Bearer")
            return FindUserIdFromBearer(context);

        _logger.LogWarning("Unrecognized authentication scheme: {Scheme}", scheme);

        return null;
    }

    private static string? FindUserIdFromApiKey(HttpContext context) =>
        context.Request.Headers.TryGetValue("X-UserId", out var userIdValue) ? userIdValue.ToString() : null;

    private static string? FindUserIdFromBearer(HttpContext context) => context.User.GetUserId();
}
