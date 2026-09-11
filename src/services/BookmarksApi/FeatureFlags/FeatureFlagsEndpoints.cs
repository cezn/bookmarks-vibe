using Microsoft.FeatureManagement;

namespace BookmarksApi.FeatureFlags;

public static class BookmarksEndpoints
{
    public static void MapFeatureFlagsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/flag/{name}", GetFeatureFlag);
    }

    private static async Task<IResult> GetFeatureFlag(string name, IFeatureManager featureManager)
    {
        return Results.Ok(
            await Task.WhenAll(
                featureManager.IsEnabledAsync(name),
                featureManager.IsEnabledAsync(name),
                featureManager.IsEnabledAsync(name),
                featureManager.IsEnabledAsync(name),
                featureManager.IsEnabledAsync(name),
                featureManager.IsEnabledAsync(name),
                featureManager.IsEnabledAsync(name)
            )
        );
    }
}
