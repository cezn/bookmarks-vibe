using System.Diagnostics;
using Npgsql;
using ServiceDefaults;

namespace BookmarksApi.Shared;

public static class NpgsqlConnectionExtensions
{
    public static async Task<NpgsqlConnection> OpenTraceAsync(
        this NpgsqlConnection self,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = Monitoring.ActivitySource.StartActivity("OpenConnectionTraceAsync", ActivityKind.Internal);
        activity?.SetTag("ConnectionString", self.ConnectionString);
        await self.OpenAsync(cancellationToken);
        activity?.SetTag("ProcessId", self.ProcessID);

        return self;
    }
}
