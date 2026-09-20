using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BookmarksApi.Bookmarks;

public static class BookmarksServiceCollection
{
    public static IServiceCollection AddBookmarks(this IServiceCollection services, IConfiguration config)
    {
        services.AddKeyedScoped<IBookmarkRepository, BookmarkRepository>(
            "rw",
            (sp, o) => new(sp.GetRequiredKeyedService<NpgsqlConnection>("rw"))
        );
        services.AddKeyedScoped<IBookmarkRepository, BookmarkRepository>(
            "ro",
            (sp, o) => new(sp.GetRequiredKeyedService<NpgsqlConnection>("ro"))
        );

        // Register Schema Registry options
        services
            .AddOptionsWithValidateOnStart<SchemaRegistryOptions>()
            .Bind(config.GetRequiredSection("Bookmarks:SchemaRegistry"));

        // Register Schema Registry client
        services.AddSingleton<ISchemaRegistryClient>(sp => new CachedSchemaRegistryClient(
            new SchemaRegistryConfig { Url = sp.GetRequiredService<IOptions<SchemaRegistryOptions>>().Value.Url }
        ));

        // Pin the subject-name strategy to Topic so the serdes do not probe
        // /associations/resources/-/... on every serialize/deserialize (which 404s
        // when no associations are registered and shows up as a failed span in traces).
        services.AddSingleton(new ProtobufSerializerConfig { SubjectNameStrategy = SubjectNameStrategy.Topic });
        services.AddSingleton(new ProtobufDeserializerConfig { SubjectNameStrategy = SubjectNameStrategy.Topic });

        // Register Protobuf serializers
        services.AddSingleton(typeof(ProtobufSerializer<>));
        services.AddSingleton(typeof(ProtobufDeserializer<>));

        services.AddSingleton<BookmarkOutboxMessageMapper>();
        services.AddSingleton<EdgeBookmarksParser>();

        return services;
    }
}
