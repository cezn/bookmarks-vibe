using System.Data.Common;

namespace BookmarksApi.Shared;

internal static class DbCommandExtensions
{
    private static readonly AsyncLocal<string?> CommandName = new();

    public static string? GetCommandName(DbCommand _) => CommandName.Value;

    public static async Task<DbDataReader> ExecuteReaderWithSpanNameAsync(
        this DbCommand command,
        string spanName,
        CancellationToken ct = default
    )
    {
        var previousValue = CommandName.Value;
        CommandName.Value = spanName;

        try
        {
            return await command.ExecuteReaderAsync(ct);
        }
        finally
        {
            CommandName.Value = previousValue;
        }
    }

    public static async Task<object?> ExecuteScalarWithSpanNameAsync(
        this DbCommand command,
        string spanName,
        CancellationToken ct = default
    )
    {
        var previousValue = CommandName.Value;
        CommandName.Value = spanName;

        try
        {
            return await command.ExecuteScalarAsync(ct);
        }
        finally
        {
            CommandName.Value = previousValue;
        }
    }

    public static async Task<int> ExecuteNonQueryWithSpanNameAsync(
        this DbCommand command,
        string spanName,
        CancellationToken ct = default
    )
    {
        var previousValue = CommandName.Value;
        CommandName.Value = spanName;

        try
        {
            return await command.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            CommandName.Value = previousValue;
        }
    }
}
