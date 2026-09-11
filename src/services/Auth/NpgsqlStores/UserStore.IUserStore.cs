using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Auth.NpgsqlStores;

public partial class UserStore
{
    public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO aspnet_users
            (id, user_name, normalized_user_name, email, normalized_email, email_confirmed,
             password_hash, security_stamp, concurrency_stamp, phone_number, phone_number_confirmed,
             two_factor_enabled, lockout_end, lockout_enabled, access_failed_count, authenticator_key)
            VALUES
            ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16)
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(user.UserName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.NormalizedUserName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.Email ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.NormalizedEmail ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.EmailConfirmed);
        command.Parameters.AddWithValue(user.PasswordHash ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.SecurityStamp ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.ConcurrencyStamp ?? Guid.NewGuid().ToString());
        command.Parameters.AddWithValue(user.PhoneNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.PhoneNumberConfirmed);
        command.Parameters.AddWithValue(user.TwoFactorEnabled);
        command.Parameters.AddWithValue(user.LockoutEnd ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.LockoutEnabled);
        command.Parameters.AddWithValue(user.AccessFailedCount);
        command.Parameters.AddWithValue(user.AuthenticatorKey ?? (object)DBNull.Value);
        await command.ExecuteNonQueryWithSpanNameAsync("CreateUser");

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM aspnet_users WHERE id = $1";
        command.Parameters.AddWithValue(user.Id ?? "");
        await command.ExecuteNonQueryWithSpanNameAsync("DeleteUser");

        return IdentityResult.Success;
    }

    public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, user_name, normalized_user_name, email, normalized_email, email_confirmed,
                password_hash, security_stamp, concurrency_stamp, phone_number, phone_number_confirmed,
                two_factor_enabled, lockout_end, lockout_enabled, access_failed_count, authenticator_key
            FROM aspnet_users
            WHERE id = $1
            """;

        command.Parameters.AddWithValue(userId);
        using var reader = await command.ExecuteReaderWithSpanNameAsync("FindUserById");

        return await reader.ReadAsync(cancellationToken) ? MapUserFromReader(reader) : null;
    }

    public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedUserName))
            return null;

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, user_name, normalized_user_name, email, normalized_email, email_confirmed,
                password_hash, security_stamp, concurrency_stamp, phone_number, phone_number_confirmed,
                two_factor_enabled, lockout_end, lockout_enabled, access_failed_count, authenticator_key
            FROM aspnet_users
            WHERE normalized_user_name = $1
            """;
        command.Parameters.AddWithValue(normalizedUserName);
        using var reader = await command.ExecuteReaderWithSpanNameAsync("FindUserByName");

        return await reader.ReadAsync(cancellationToken) ? MapUserFromReader(reader) : null;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE aspnet_users
            SET user_name = $2,
                normalized_user_name = $3,
                email = $4,
                normalized_email = $5,
                email_confirmed = $6,
                password_hash = $7,
                security_stamp = $8,
                concurrency_stamp = $9,
                phone_number = $10,
                phone_number_confirmed = $11,
                two_factor_enabled = $12,
                lockout_end = $13,
                lockout_enabled = $14,
                access_failed_count = $15,
                authenticator_key = $16
            WHERE id = $1
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(user.UserName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.NormalizedUserName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.Email ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.NormalizedEmail ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.EmailConfirmed);
        command.Parameters.AddWithValue(user.PasswordHash ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.SecurityStamp ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.ConcurrencyStamp ?? Guid.NewGuid().ToString());
        command.Parameters.AddWithValue(user.PhoneNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.PhoneNumberConfirmed);
        command.Parameters.AddWithValue(user.TwoFactorEnabled);
        command.Parameters.AddWithValue(user.LockoutEnd ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(user.LockoutEnabled);
        command.Parameters.AddWithValue(user.AccessFailedCount);
        command.Parameters.AddWithValue(user.AuthenticatorKey ?? (object)DBNull.Value);
        await command.ExecuteNonQueryWithSpanNameAsync("UpdateUser");

        return IdentityResult.Success;
    }

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName);

    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(
        ApplicationUser user,
        string? normalizedName,
        CancellationToken cancellationToken
    )
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // Npgsql connections are managed by using statements, nothing to dispose here
    }
}
