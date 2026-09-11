using System.Text.Json;
using BookmarksApi.Shared;
using Npgsql;

namespace BookmarksApi.Setup;

public static class NpgsqlExtensions
{
    public static IServiceCollection AddNpgsql(
        this IServiceCollection services,
        string? connectionStringRw,
        string? connectionStringRo,
        IWebHostEnvironment environment
    )
    {
        services.AddNpgsqlDataSource(
            connectionString: new NpgsqlConnectionStringBuilder(
                connectionStringRw ?? throw new InvalidOperationException("Connection string 'BookmarksRw' not found.")
            )
            {
                MaxAutoPrepare = 500,
                AutoPrepareMinUsages = 3,
                ApplicationName = environment.ApplicationName,
                CommandTimeout = 30,
                Enlist = false,
                NoResetOnClose = true,
            }.ConnectionString,
            dataSourceBuilderAction: builder =>
                builder
                    .EnableParameterLogging(environment.IsDevelopment() || environment.IsEnvironment("Test"))
                    .ConfigureTracing(x =>
                    {
                        x.ConfigureCommandSpanNameProvider(DbCommandExtensions.GetCommandName);
                        x.ConfigureCommandEnrichmentWithParameters(environment);
                    }),
            serviceKey: "rw",
            connectionLifetime: ServiceLifetime.Scoped
        );
        services.AddNpgsqlDataSource(
            connectionString: new NpgsqlConnectionStringBuilder(
                connectionStringRo ?? throw new InvalidOperationException("Connection string 'BookmarksRo' not found.")
            )
            {
                MaxAutoPrepare = 500,
                AutoPrepareMinUsages = 3,
                ApplicationName = environment.ApplicationName,
                CommandTimeout = 30,
                Enlist = false,
                NoResetOnClose = true,
            }.ConnectionString,
            dataSourceBuilderAction: builder =>
                builder
                    .EnableParameterLogging(environment.IsDevelopment())
                    .ConfigureTracing(x =>
                    {
                        x.ConfigureCommandSpanNameProvider(DbCommandExtensions.GetCommandName);
                        x.ConfigureCommandEnrichmentWithParameters(environment);
                    }),
            serviceKey: "ro",
            connectionLifetime: ServiceLifetime.Scoped
        );
        services.AddOpenTelemetry().WithMetrics(x => x.AddMeter("Npgsql")).WithTracing(x => x.AddSource("Npgsql"));

        return services;
    }
}

public static class NpgsqlTracingOptionsBuilderExtensions
{
    public static NpgsqlTracingOptionsBuilder ConfigureCommandEnrichmentWithParameters(
        this NpgsqlTracingOptionsBuilder builder,
        IWebHostEnvironment environment
    ) =>
        builder.ConfigureCommandEnrichmentCallback(
            (act, cmd) =>
            {
                if (!environment.IsDevelopment() && !environment.IsEnvironment("Test"))
                    return;

                var sql = act.GetTagItem("db.query.text") as string;
                if (string.IsNullOrEmpty(sql))
                    return;

                act.SetTag("db.query.text", ReplaceSqlPlaceholders(sql, cmd.Parameters));
            }
        );

    private static string ReplaceSqlPlaceholders(string sql, NpgsqlParameterCollection parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            var p = parameters[i];
            var placeholder = $"${i + 1}";
            var value = p.Value ?? DBNull.Value;
            var replacement = value is string s
                ? $"'{s.Replace("'", "''")}'"
                : JsonSerializer.Serialize(value, JsonSerializerOptions.Web);

            sql = sql.Replace(placeholder, replacement);
        }

        return sql;
    }
}
