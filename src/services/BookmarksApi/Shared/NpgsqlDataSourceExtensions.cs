using System.Diagnostics;
using Npgsql;
using ServiceDefaults;

namespace BookmarksApi.Shared;

public static class NpgsqlDataSourceExtensions
{
    public static async ValueTask<NpgsqlConnection> OpenConnectionTraceAsync(
        this NpgsqlDataSource self,
        CancellationToken ct = default
    )
    {
        using var activity = Monitoring.ActivitySource.StartActivity("OpenConnectionTraceAsync", ActivityKind.Internal);
        activity?.SetTag("ConnectionString", self.ConnectionString);
        var con = await self.OpenConnectionAsync(ct);
        activity?.SetTag("ProcessId", con.ProcessID);
        return con;
    }
}
