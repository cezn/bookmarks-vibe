using Aspire.Hosting.ApplicationModel;
using Cezn.Aspire.Hosting;
using Cezn.Aspire.Hosting.KafkaConnect.Connectors.PostgresqlSource;
using Projects;

namespace Aspire.Hosting;

#pragma warning disable ASPIREPROCESSCOMMAND001

public static class BookmarksServiceExtensions
{
    public static IResourceBuilder<ProjectResource> AddBookmarks(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<PostgresServerResource> postgres,
        IResourceBuilder<KafkaConnectResource> kafkaConnect,
        IResourceBuilder<SchemaRegistryResource> schemaRegistry,
        IResourceBuilder<ParameterResource> jwtKey,
        IResourceBuilder<SwaggerUIResource> swagger,
        IResourceBuilder<ContainerResource> otelCollector
    )
    {
        var bookmarksDb = postgres.AddDatabase("bookmarksdb");
        var bookmarksMigrations = bookmarksDb
            .AddGrateMigrations("bookmarks-migrations", "../src/services/BookmarksApi/migrations")
            .WithEnvironment("local")
            .RunMigrationsOnStart();

        kafkaConnect
            .AddPostgresDebeziumConnector(
                "bookmarks-outbox",
                bookmarksDb,
                options => options.WithOutboxCdc(schemaRegistry.GetEndpoint("http"))
            )
            .WaitForCompletion(bookmarksMigrations);

        var bookmarksApi = builder
            .AddProject<BookmarksApi>(name)
            .WithReference(bookmarksDb, connectionName: "BookmarksRw")
            .WithReference(bookmarksDb, connectionName: "BookmarksRo")
            .WaitForCompletion(bookmarksMigrations)
            .WithEnvironment("Jwt__Key", jwtKey)
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables["Bookmarks__SchemaRegistry__Url"] = schemaRegistry.GetEndpoint("http");
            })
            .WithSwagger(swagger, "BookmarksApi")
            .WithOtlpExporterViaCollector(otelCollector);

        return bookmarksApi;
    }
}
