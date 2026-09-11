using PushNotificationsService.Shared;

namespace PushNotificationsService.Setup;

public static class NpgsqlExtensions
{
    public static IServiceCollection AddNpgsql(
        this IServiceCollection services,
        string? connectionString,
        IWebHostEnvironment environment
    )
    {
        services.AddNpgsqlDataSource(
            connectionString ?? throw new InvalidOperationException("Connection string 'PushNotifications' not found."),
            dataSourceBuilderAction: builder =>
                builder
                    .EnableParameterLogging(environment.IsDevelopment() || environment.IsEnvironment("Test"))
                    .ConfigureTracing(x => x.ConfigureCommandSpanNameProvider(DbCommandExtensions.GetCommandName))
        );
        services.AddOpenTelemetry().WithMetrics(x => x.AddMeter("Npgsql")).WithTracing(x => x.AddSource("Npgsql"));

        return services;
    }
}
