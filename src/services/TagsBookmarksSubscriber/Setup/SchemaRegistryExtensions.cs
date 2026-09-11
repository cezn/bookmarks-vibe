using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace TagsBookmarksSubscriber.Setup;

public static class Schema
{
    public static IServiceCollection AddSchemaRegistry(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ISchemaRegistryClient>(sp => new CachedSchemaRegistryClient(
            new SchemaRegistryConfig { Url = config["SchemaRegistryUrl"] }
        ));
        services.AddSingleton(typeof(ProtobufSerializer<>));
        services.AddSingleton(typeof(ProtobufDeserializer<>));

        return services;
    }
}
