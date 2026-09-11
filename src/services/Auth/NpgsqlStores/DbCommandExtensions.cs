using Npgsql;

namespace Auth.NpgsqlStores;

internal static class DbCommandExtensions
{
    private static readonly AsyncLocal<string?> CommandName = new();

    public static string? GetCommandName(NpgsqlCommand _) => CommandName.Value;

    public static async Task<NpgsqlDataReader> ExecuteReaderWithSpanNameAsync(
        this NpgsqlCommand command,
        string spanName
    )
    {
        var previousValue = CommandName.Value;
        CommandName.Value = spanName;

        try
        {
            return await command.ExecuteReaderAsync();
        }
        finally
        {
            CommandName.Value = previousValue;
        }
    }

    public static async Task<object?> ExecuteScalarWithSpanNameAsync(this NpgsqlCommand command, string spanName)
    {
        var previousValue = CommandName.Value;
        CommandName.Value = spanName;

        try
        {
            return await command.ExecuteScalarAsync();
        }
        finally
        {
            CommandName.Value = previousValue;
        }
    }

    public static async Task<int> ExecuteNonQueryWithSpanNameAsync(this NpgsqlCommand command, string spanName)
    {
        var previousValue = CommandName.Value;
        CommandName.Value = spanName;

        try
        {
            return await command.ExecuteNonQueryAsync();
        }
        finally
        {
            CommandName.Value = previousValue;
        }
    }

    public static NpgsqlDataReader ExecuteReaderWithSpanName(this NpgsqlCommand command, string spanName)
    {
        var previousValue = CommandName.Value;
        CommandName.Value = spanName;

        try
        {
            return command.ExecuteReader();
        }
        finally
        {
            CommandName.Value = previousValue;
        }
    }

    public static object? ExecuteScalarWithSpanName(this NpgsqlCommand command, string spanName)
    {
        var previousValue = CommandName.Value;
        CommandName.Value = spanName;

        try
        {
            return command.ExecuteScalar();
        }
        finally
        {
            CommandName.Value = previousValue;
        }
    }

    public static int ExecuteNonQueryWithSpanName(this NpgsqlCommand command, string spanName)
    {
        var previousValue = CommandName.Value;
        CommandName.Value = spanName;

        try
        {
            return command.ExecuteNonQuery();
        }
        finally
        {
            CommandName.Value = previousValue;
        }
    }
}
