using Aspire.Hosting.ApplicationModel;
using Cezn.Aspire.Hosting;
using Projects;

namespace Aspire.Hosting;

public static class TagsBookmarksSubscriberServiceExtensions
{
    public static IResourceBuilder<ProjectResource> AddTagsBookmarksSubscriber(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<KafkaServerResource> kafka,
        IResourceBuilder<SchemaRegistryResource> schemaRegistry,
        IResourceBuilder<ProjectResource> bookmarksApi,
        IResourceBuilder<ParameterResource> bookmarksApiKey,
        IResourceBuilder<ContainerResource> otelCollector
    )
    {
        var subscriber = builder
            .AddProject<TagsBookmarksSubscriber>(name)
            .WithReference(kafka)
            .WaitFor(kafka)
            .WithReference(schemaRegistry)
            .WaitFor(schemaRegistry)
            .WithReference(bookmarksApi)
            .WaitFor(bookmarksApi)
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables["SchemaRegistryUrl"] = schemaRegistry.GetEndpoint("http");
                ctx.EnvironmentVariables["BookmarksApi__Url"] = bookmarksApi.GetEndpoint("http");
            })
            .WithEnvironment("BookmarksApi__ApiKey", bookmarksApiKey)
            .WithOtlpExporterViaCollector(otelCollector);

        return subscriber;
    }
}
