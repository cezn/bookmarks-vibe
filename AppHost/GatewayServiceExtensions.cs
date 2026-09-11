using Aspire.Hosting.JavaScript;
using Projects;

namespace Aspire.Hosting;

public static class GatewayServiceExtensions
{
    public static IResourceBuilder<ProjectResource> AddGateway(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<RedisResource> redis,
        IResourceBuilder<ParameterResource> jwtKey,
        IResourceBuilder<ProjectResource> bookmarksApi,
        IResourceBuilder<ProjectResource> auth,
        IResourceBuilder<ViteAppResource> ui,
        IResourceBuilder<ContainerResource> otelCollector,
        IResourceBuilder<ProjectResource>? staticAssets = null
    )
    {
        var gateway = builder
            .AddProject<Gateway>(name)
            .WithExternalHttpEndpoints()
            .WithReference(redis)
            .WithEnvironment("Jwt__Key", jwtKey)
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables["ReverseProxy__Clusters__bookmarks__Destinations__destination1__Address"] =
                    bookmarksApi.GetEndpoint("http");
                ctx.EnvironmentVariables["ReverseProxy__Clusters__auth__Destinations__destination1__Address"] =
                    auth.GetEndpoint("http");
                ctx.EnvironmentVariables["ReverseProxy__Clusters__static-assets__Destinations__destination1__Address"] =
                    staticAssets?.GetEndpoint("http") ?? ui.GetEndpoint("http");
                ctx.EnvironmentVariables[
                    "ReverseProxy__Clusters__otel-collector__Destinations__destination1__Address"
                ] = otelCollector.GetEndpoint("http");
            })
            .WithUrlForEndpoint("http", url => url.DisplayText = "Gateway");

        return gateway;
    }
}
