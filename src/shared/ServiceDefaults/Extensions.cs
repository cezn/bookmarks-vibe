using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using ServiceDefaults;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.ClearProviders().AddCustomConsoleFormatter();
        builder.AddCustomOpenTelemetry();
        builder.AddCustomHealthChecks();

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapDebugEndpoints();
        app.MapHealthCheckEndpoints();

        return app;
    }
}
