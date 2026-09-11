using System.Text.Json;
using Auth.NpgsqlStores;
using Npgsql;

namespace Auth.Setup;

public static class NpgsqlExtensions
{
    public static IHostApplicationBuilder AddNpgsql(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDataSource(
            "authdb",
            configureDataSourceBuilder: dsBuilder =>
                dsBuilder
                    .EnableParameterLogging(
                        builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Test")
                    )
                    .ConfigureTracing(x =>
                    {
                        x.ConfigureCommandSpanNameProvider(DbCommandExtensions.GetCommandName);
                        x.ConfigureCommandEnrichmentWithParameters(builder.Environment);
                    })
        );

        return builder;
    }
}

public static class NpgsqlTracingOptionsBuilderExtensions
{
    public static NpgsqlTracingOptionsBuilder ConfigureCommandEnrichmentWithParameters(
        this NpgsqlTracingOptionsBuilder builder,
        IHostEnvironment environment
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
