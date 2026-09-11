using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Auth.NpgsqlStores;

public partial class UserStore
{
    public async Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            throw new ArgumentException("Role name cannot be null or empty", nameof(roleName));

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO aspnet_user_roles (user_id, role_id)
            SELECT $1, id FROM aspnet_roles WHERE normalized_name = $2
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(roleName);
        await command.ExecuteNonQueryWithSpanNameAsync("AddUserToRole");
    }

    public async Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            throw new ArgumentException("Role name cannot be null or empty", nameof(roleName));

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM aspnet_user_roles
            WHERE user_id = $1 AND role_id = (SELECT id FROM aspnet_roles WHERE normalized_name = $2)
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(roleName);
        await command.ExecuteNonQueryWithSpanNameAsync("RemoveUserFromRole");
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = new List<string>();

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.name
            FROM aspnet_roles r
            INNER JOIN aspnet_user_roles ur ON r.id = ur.role_id
            WHERE ur.user_id = $1
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        using var reader = await command.ExecuteReaderWithSpanNameAsync("GetUserRoles");
        while (await reader.ReadAsync(cancellationToken))
            roles.Add(reader.GetString(0));

        return roles;
    }

    public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM aspnet_user_roles ur
            INNER JOIN aspnet_roles r ON ur.role_id = r.id
            WHERE ur.user_id = $1 AND r.normalized_name = $2
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(roleName);
        var result = await command.ExecuteScalarWithSpanNameAsync("IsUserInRole");
        return (long)(result ?? 0) > 0;
    }

    public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var users = new List<ApplicationUser>();

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DISTINCT u.id, u.user_name, u.normalized_user_name, u.email, u.normalized_email,
                u.email_confirmed, u.password_hash, u.security_stamp, u.concurrency_stamp,
                u.phone_number, u.phone_number_confirmed, u.two_factor_enabled,
                u.lockout_end, u.lockout_enabled, u.access_failed_count, u.authenticator_key
            FROM aspnet_users u
            INNER JOIN aspnet_user_roles ur ON u.id = ur.user_id
            INNER JOIN aspnet_roles r ON ur.role_id = r.id
            WHERE r.name = $1
            """;
        command.Parameters.AddWithValue(roleName);
        using var reader = await command.ExecuteReaderWithSpanNameAsync("GetUsersInRole");
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(MapUserFromReader(reader));
        }

        return users;
    }
}
