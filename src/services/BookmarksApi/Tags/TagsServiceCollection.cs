namespace BookmarksApi.Tags;

public static class TagsServiceCollection
{
    public static IServiceCollection AddTags(this IServiceCollection services, IConfiguration config)
    {
        services.AddSignalR();
        return services;
    }
}
