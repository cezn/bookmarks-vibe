using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class ElasticsearchServiceExtensions
{
    public static IResourceBuilder<ContainerResource> AddElasticsearch(
        this IDistributedApplicationBuilder builder,
        string name
    )
    {
        var elasticsearch = builder
            .AddContainer(name: name, image: "docker.elastic.co/elasticsearch/elasticsearch", tag: "9.3.0")
            .WithHttpEndpoint(name: "http", targetPort: 9200)
            .WithEndpoint(name: "transport", targetPort: 9300, protocol: System.Net.Sockets.ProtocolType.Tcp)
            .WithEnvironment("discovery.type", "single-node")
            .WithEnvironment("xpack.security.enabled", "false")
            .WithEnvironment("ES_JAVA_OPTS", "-Xms1g -Xmx1g")
            .WithVolume("elasticsearch-data", "/usr/share/elasticsearch/data")
            .WithLifetime(ContainerLifetime.Persistent);

        return elasticsearch;
    }
}
