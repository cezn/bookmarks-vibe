namespace Auth.NpgsqlStores;

public partial class UserStore
{
    public async Task<string?> GetTokenAsync(
        ApplicationUser user,
        string loginProvider,
        string name,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(loginProvider) || string.IsNullOrWhiteSpace(name))
            return null;

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT value
            FROM aspnet_user_tokens
            WHERE user_id = $1 AND login_provider = $2 AND name = $3
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(loginProvider);
        command.Parameters.AddWithValue(name);

        var result = await command.ExecuteScalarWithSpanNameAsync("GetUserAuthenticationToken");
        return result == null || result == DBNull.Value ? null : Convert.ToString(result);
    }

    public async Task RemoveTokenAsync(
        ApplicationUser user,
        string loginProvider,
        string name,
        CancellationToken cancellationToken
    )
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM aspnet_user_tokens
            WHERE user_id = $1 AND login_provider = $2 AND name = $3
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(loginProvider ?? "");
        command.Parameters.AddWithValue(name ?? "");
        await command.ExecuteNonQueryWithSpanNameAsync("RemoveUserAuthenticationToken");
    }

    public async Task SetTokenAsync(
        ApplicationUser user,
        string loginProvider,
        string name,
        string? value,
        CancellationToken cancellationToken
    )
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO aspnet_user_tokens (user_id, login_provider, name, value)
            VALUES ($1, $2, $3, $4)
            ON CONFLICT (user_id, login_provider, name)
            DO UPDATE SET value = EXCLUDED.value
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(loginProvider ?? "");
        command.Parameters.AddWithValue(name ?? "");
        command.Parameters.AddWithValue(value ?? (object)DBNull.Value);
        await command.ExecuteNonQueryWithSpanNameAsync("SetUserAuthenticationToken");
    }
}
