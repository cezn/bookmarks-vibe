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

        // Pin the subject-name strategy to Topic so the serdes do not probe
        // /associations/resources/-/... on every serialize/deserialize (which 404s
        // when no associations are registered and shows up as a failed span in traces).
        services.AddSingleton(new ProtobufSerializerConfig { SubjectNameStrategy = SubjectNameStrategy.Topic });
        services.AddSingleton(new ProtobufDeserializerConfig { SubjectNameStrategy = SubjectNameStrategy.Topic });

        services.AddSingleton(typeof(ProtobufSerializer<>));
        services.AddSingleton(typeof(ProtobufDeserializer<>));

        return services;
    }
}
