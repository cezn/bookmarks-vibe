using Microsoft.FeatureManagement;

namespace BookmarksApi.FeatureFlags;

public static class MyClass
{
    public static IServiceCollection AddFeatureFlags(this IServiceCollection services)
    {
        services.AddFeatureManagement();

        return services;
    }
}
