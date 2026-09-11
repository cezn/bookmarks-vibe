using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Auth.NpgsqlStores;

public partial class UserStore
{
    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return null;

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, user_name, normalized_user_name, email, normalized_email,
                email_confirmed, password_hash, security_stamp, concurrency_stamp,
                phone_number, phone_number_confirmed, two_factor_enabled,
                lockout_end, lockout_enabled, access_failed_count, authenticator_key
            FROM aspnet_users
            WHERE normalized_email = $1
            """;
        command.Parameters.AddWithValue(normalizedEmail);
        using var reader = await command.ExecuteReaderWithSpanNameAsync("FindUserByEmail");

        return await reader.ReadAsync(cancellationToken) ? MapUserFromReader(reader) : null;
    }

    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(
        ApplicationUser user,
        string? normalizedEmail,
        CancellationToken cancellationToken
    )
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }
}
