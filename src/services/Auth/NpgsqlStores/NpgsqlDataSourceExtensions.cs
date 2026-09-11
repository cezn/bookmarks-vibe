using System.Diagnostics;
using Npgsql;
using ServiceDefaults;

namespace Auth.NpgsqlStores;

public static class NpgsqlDataSourceExtensions
{
    public static async ValueTask<NpgsqlConnection> OpenConnectionTraceAsync(
        this NpgsqlDataSource self,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = Monitoring.ActivitySource.StartActivity("OpenConnectionTraceAsync", ActivityKind.Internal);
        activity?.SetTag("ConnectionString", self.ConnectionString);
        var con = await self.OpenConnectionAsync(cancellationToken);
        activity?.SetTag("ProcessId", con.ProcessID);
        return con;
    }

    public static NpgsqlConnection OpenConnectionTrace(this NpgsqlDataSource self)
    {
        using var activity = Monitoring.ActivitySource.StartActivity("OpenConnectionTrace", ActivityKind.Internal);
        activity?.SetTag("ConnectionString", self.ConnectionString);
        var con = self.OpenConnection();
        activity?.SetTag("ProcessId", con.ProcessID);
        return con;
    }
}
