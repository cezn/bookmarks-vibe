using System.Data.Common;

namespace PushNotificationsService.Shared;

public static class DbCommandExtensions
{
    public static async Task<DbDataReader> ExecuteReaderWithSpanNameAsync(this DbCommand cmd, string spanName)
    {
        return await cmd.ExecuteReaderAsync();
    }

    public static string GetCommandName(DbCommand cmd)
    {
        return cmd.CommandText.Split('\n')[0].Split(' ').Take(10).Aggregate((a, b) => $"{a} {b}");
    }
}
