namespace TagsBookmarksSubscriber.Setup;

public static class BookmarksClientExtensions
{
    public static IServiceCollection AddBookmarksClient(this IServiceCollection services, IConfiguration config)
    {
        services
            .AddHttpClient<BookmarksApiClient>(client =>
            {
                var apiKey =
                    config["BookmarksApi:ApiKey"]
                    ?? throw new InvalidOperationException("BookmarksApi:ApiKey configuration is required.");

                client.BaseAddress = new Uri(
                    config["BookmarksApi:Url"]
                        ?? throw new InvalidOperationException("BookmarksApi:BaseUrl configuration is required.")
                );
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            })
            .AddStandardResilienceHandler(config.GetSection("BookmarksApi:Resilience"));

        return services;
    }
}
