using System.Diagnostics;
using OpenTelemetry.Trace;
using ServiceDefaults;

namespace BookmarksApi.Setup;

static class WebApplicationExtensions
{
    public static void RunWithTrace(this WebApplication app)
    {
        _ = app.Services.GetService<TracerProvider>(); // Necessary to start activity listeners.
        var activity = Monitoring.ActivitySource.StartActivity("WebApplication.Run", ActivityKind.Internal);
        app.Lifetime.ApplicationStarted.Register(() => activity?.Stop());

        app.Run();
    }
}
