using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Gateway.Setup;

public class RateLimiterOptions
{
    public const string SectionName = "RateLimiter";

    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 5000;

    [Range(
        typeof(TimeSpan),
        "00:00:01",
        "00:00:10",
        ErrorMessage = "Lifetime must be between {1} and {2}",
        MinimumIsExclusive = false,
        MaximumIsExclusive = false
    )]
    public TimeSpan Window { get; set; }

    [Required]
    public QueueProcessingOrder? QueueProcessingOrder { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "QueueLimit cannot be negative")]
    public int QueueLimit { get; set; } = 0;
}

public static class RateLimiterExtensions
{
    public static IServiceCollection AddCustomRateLimiter(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptionsWithValidateOnStart<RateLimiterOptions>()
            .Bind(configuration.GetSection(RateLimiterOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddRateLimiter(options =>
        {
            options.OnRejected = HandleOnRejected;
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                        _ =>
                        {
                            var rateLimiterOptions = context
                                .RequestServices.GetRequiredService<IOptions<RateLimiterOptions>>()
                                .Value;
                            return new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = rateLimiterOptions.PermitLimit,
                                Window = rateLimiterOptions.Window,
                                QueueProcessingOrder = rateLimiterOptions.QueueProcessingOrder!.Value,
                                QueueLimit = rateLimiterOptions.QueueLimit,
                            };
                        }
                    )
                )
            );
        });
        return services;
    }

    private static async ValueTask HandleOnRejected(
        Microsoft.AspNetCore.RateLimiting.OnRejectedContext context,
        CancellationToken ct
    )
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(
                NumberFormatInfo.InvariantInfo
            );

        if (context.HttpContext.RequestServices.GetService<IProblemDetailsService>() is { } problemDetailsService)
            await problemDetailsService.TryWriteAsync(
                new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails()
                    {
                        Title = "Too Many Requests",
                        Detail = "You have exceeded the number of requests allowed. Please try again later.",
                        Status = StatusCodes.Status429TooManyRequests,
                        Type = "https://httpstatuses.com/429",
                    },
                }
            );
        else
            await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", ct);
    }
}
