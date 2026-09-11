using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Auth.NpgsqlStores;

public partial class UserStore
{
    public async Task AddLoginAsync(ApplicationUser user, UserLoginInfo login, CancellationToken cancellationToken)
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO aspnet_user_logins (login_provider, provider_key, provider_display_name, user_id)
            VALUES ($1, $2, $3, $4)
            """;
        command.Parameters.AddWithValue(login.LoginProvider ?? "");
        command.Parameters.AddWithValue(login.ProviderKey ?? "");
        command.Parameters.AddWithValue(login.ProviderDisplayName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.Id ?? "");
        await command.ExecuteNonQueryWithSpanNameAsync("AddUserLogin");
    }

    public async Task RemoveLoginAsync(
        ApplicationUser user,
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken
    )
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM aspnet_user_logins
            WHERE user_id = $1 AND login_provider = $2 AND provider_key = $3
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(loginProvider ?? "");
        command.Parameters.AddWithValue(providerKey ?? "");
        await command.ExecuteNonQueryWithSpanNameAsync("RemoveUserLogin");
    }

    public async Task<IList<UserLoginInfo>> GetLoginsAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var logins = new List<UserLoginInfo>();

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT login_provider, provider_key, provider_display_name
            FROM aspnet_user_logins
            WHERE user_id = $1
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        using var reader = await command.ExecuteReaderWithSpanNameAsync("GetUserLogins");
        while (await reader.ReadAsync(cancellationToken))
            logins.Add(
                new UserLoginInfo(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2)
                )
            );

        return logins;
    }

    public async Task<ApplicationUser?> FindByLoginAsync(
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(loginProvider) || string.IsNullOrWhiteSpace(providerKey))
            return null;

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT u.id, u.user_name, u.normalized_user_name, u.email, u.normalized_email,
                u.email_confirmed, u.password_hash, u.security_stamp, u.concurrency_stamp,
                u.phone_number, u.phone_number_confirmed, u.two_factor_enabled,
                u.lockout_end, u.lockout_enabled, u.access_failed_count, u.authenticator_key
            FROM aspnet_users u
            INNER JOIN aspnet_user_logins ul ON u.id = ul.user_id
            WHERE ul.login_provider = $1 AND ul.provider_key = $2
            """;
        command.Parameters.AddWithValue(loginProvider);
        command.Parameters.AddWithValue(providerKey);
        using var reader = await command.ExecuteReaderWithSpanNameAsync("FindUserByLogin");

        return await reader.ReadAsync(cancellationToken) ? MapUserFromReader(reader) : null;
    }
}
