using Cezn.Aspire.Hosting;
using Projects;

namespace Aspire.Hosting;

#pragma warning disable ASPIREPROCESSCOMMAND001

public static class AuthServiceExtensions
{
    public static IResourceBuilder<ProjectResource> AddAuth(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<PostgresServerResource> postgres,
        IResourceBuilder<RedisResource> redis,
        IResourceBuilder<ContainerResource> mailhog,
        IResourceBuilder<SwaggerUIResource> swagger,
        IResourceBuilder<ContainerResource> otelCollector
    )
    {
        var authDb = postgres.AddDatabase("authdb");

        var authMigrations = authDb
            .AddGrateMigrations("auth-migrations", "../src/services/Auth/migrations")
            .WithEnvironment("local")
            .RunMigrationsOnStart();

        var auth = builder
            .AddProject<Auth>(name)
            .WithReference(redis)
            .WithReference(authDb)
            .WaitForCompletion(authMigrations)
            .WithEnvironment(ctx =>
            {
                var mailhogSmtpEndpoint = mailhog.GetEndpoint("smtp");
                ctx.EnvironmentVariables["AuthMessageSenderOptions__SmtpHost"] = mailhogSmtpEndpoint.Property(
                    EndpointProperty.Host
                );
                ctx.EnvironmentVariables["AuthMessageSenderOptions__SmtpPort"] = mailhogSmtpEndpoint.Property(
                    EndpointProperty.Port
                );
            })
            .WithProcessCommand(
                commandName: "seed-admin",
                displayName: "Seed Admin User",
                processSpecFactory: context =>
                {
                    var connectionString = authDb
                        .Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
                        .Result;
                    return new ProcessCommandSpec("dotnet")
                    {
                        Arguments =
                        [
                            "run",
                            "--file",
                            "../src/services/Auth/scripts/seed-admin.cs",
                            "--",
                            "-c",
                            connectionString!,
                        ],
                        EnvironmentVariables = new Dictionary<string, string>() { { "ADMIN_PASSWORD", "Secret12#" } },
                    };
                },
                commandOptions: new ProcessCommandOptions
                {
                    Description = "Seed the core admin user and assign the admin role",
                    IconName = "PersonAdd",
                    IconVariant = IconVariant.Filled,
                }
            )
            .WithSwagger(swagger, "Auth")
            .WithOtlpExporterViaCollector(otelCollector);

        return auth;
    }
}
