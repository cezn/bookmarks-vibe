using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class KibanaServiceExtensions
{
    public static IResourceBuilder<ContainerResource> AddKibana(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ContainerResource> elasticsearch
    )
    {
        var kibana = builder
            .AddContainer(name: name, image: "docker.elastic.co/kibana/kibana", tag: "9.3.0")
            .WithHttpEndpoint(name: "http", targetPort: 5601)
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables["ELASTICSEARCH_HOSTS"] = elasticsearch.GetEndpoint("http").Url;
            })
            .WaitFor(elasticsearch)
            .WithUrlForEndpoint("http", url => url.DisplayText = "Kibana");

        return kibana;
    }
}
