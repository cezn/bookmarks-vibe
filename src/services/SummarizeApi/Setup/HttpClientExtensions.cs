using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;

namespace SummarizeApi.Setup;

public static class HttpClientsExtensions
{
    public static IServiceCollection AddCustomHttpClients(this IServiceCollection services, IConfiguration config)
    {
        AddBookmarksApiClient(services, config);
        AddPageInfoLoaderClient(services);

        return services;
    }

    private static void AddBookmarksApiClient(IServiceCollection services, IConfiguration config)
    {
        services.AddKeyedSingleton<CircuitBreakerStateProvider>(nameof(BookmarksApiClient));
        services
            .AddHttpClient<BookmarksApiClient>(
                (sp, client) =>
                {
                    client.BaseAddress = new Uri(
                        config["BookmarksApi:Url"]
                            ?? throw new InvalidOperationException("BookmarksApi:Url configuration is required.")
                    );
                    client.DefaultRequestHeaders.Authorization =
                        System.Net.Http.Headers.AuthenticationHeaderValue.Parse(
                            sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request.Headers.Authorization!
                        );
                }
            )
            .AddStandardResilienceHandler(config.GetSection("BookmarksApi:Resilience"))
            .Configure(
                (opt, sp) =>
                {
                    opt.CircuitBreaker.StateProvider = sp.GetRequiredKeyedService<CircuitBreakerStateProvider>(
                        nameof(BookmarksApiClient)
                    );
                }
            );
    }

    private static void AddPageInfoLoaderClient(IServiceCollection services)
    {
        services
            .AddHttpClient<PageInfoLoader>(
                (sp, client) =>
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        (string?)
                            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36 Edg/143.0.0.0"
                    )
            )
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 5,

                    // Important for untrusted URLs
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,

                    // Prevent connection pileups
                    MaxConnectionsPerServer = 10,
                }
            )
            .AddResilienceHandler(
                "PageInfoLoader",
                (builder, ctx) =>
                {
                    builder.AddTimeout(TimeSpan.FromSeconds(10));
                }
            );
    }
}
