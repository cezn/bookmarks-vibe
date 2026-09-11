using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Auth.NpgsqlStores;

public partial class UserStore
{
    public async Task AddOrUpdatePasskeyAsync(
        ApplicationUser user,
        UserPasskeyInfo passkey,
        CancellationToken cancellationToken
    )
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO aspnet_user_passkeys
            (credential_id, user_id, data)
            VALUES ($1, $2, $3)
            ON CONFLICT (credential_id) DO UPDATE SET
                data = EXCLUDED.data
            """;
        var jsonData = SerializePasskeyToJson(passkey);
        command.Parameters.AddWithValue(passkey.CredentialId);
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(NpgsqlTypes.NpgsqlDbType.Jsonb, jsonData);

        await command.ExecuteNonQueryWithSpanNameAsync("AddOrUpdatePasskey");
    }

    public async Task<IList<UserPasskeyInfo>> GetPasskeysAsync(
        ApplicationUser user,
        CancellationToken cancellationToken
    )
    {
        var passkeys = new List<UserPasskeyInfo>();

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT credential_id, data
            FROM aspnet_user_passkeys
            WHERE user_id = $1
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        using var reader = await command.ExecuteReaderWithSpanNameAsync("GetUserPasskeys");
        while (await reader.ReadAsync(cancellationToken))
            passkeys.Add(DeserializePasskeyFromJson((byte[])reader.GetValue(0), reader.GetString(1)));

        return passkeys;
    }

    public async Task<ApplicationUser?> FindByPasskeyIdAsync(byte[] credentialId, CancellationToken cancellationToken)
    {
        if (credentialId == null || credentialId.Length == 0)
            return null;

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT u.id, u.user_name, u.normalized_user_name, u.email, u.normalized_email,
                u.email_confirmed, u.password_hash, u.security_stamp, u.concurrency_stamp,
                u.phone_number, u.phone_number_confirmed, u.two_factor_enabled,
                u.lockout_end, u.lockout_enabled, u.access_failed_count, u.authenticator_key
            FROM aspnet_users u
            INNER JOIN aspnet_user_passkeys p ON u.id = p.user_id
            WHERE p.credential_id = $1
            """;
        command.Parameters.AddWithValue(credentialId);
        using var reader = await command.ExecuteReaderWithSpanNameAsync("FindUserByPasskeyId");

        return await reader.ReadAsync(cancellationToken) ? MapUserFromReader(reader) : null;
    }

    public async Task<UserPasskeyInfo?> FindPasskeyAsync(
        ApplicationUser user,
        byte[] credentialId,
        CancellationToken cancellationToken
    )
    {
        if (credentialId == null || credentialId.Length == 0)
            return null;

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT credential_id, data
            FROM aspnet_user_passkeys
            WHERE user_id = $1 AND credential_id = $2
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(credentialId);
        using var reader = await command.ExecuteReaderWithSpanNameAsync("FindPasskey");
        return await reader.ReadAsync(cancellationToken)
            ? DeserializePasskeyFromJson((byte[])reader.GetValue(0), reader.GetString(1))
            : null;
    }

    public async Task RemovePasskeyAsync(ApplicationUser user, byte[] credentialId, CancellationToken cancellationToken)
    {
        if (credentialId == null || credentialId.Length == 0)
            return;

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM aspnet_user_passkeys
            WHERE user_id = $1 AND credential_id = $2
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(credentialId);
        await command.ExecuteNonQueryWithSpanNameAsync("RemovePasskey");
    }

    private static string SerializePasskeyToJson(UserPasskeyInfo passkey) =>
        System.Text.Json.JsonSerializer.Serialize(
            new
            {
                publicKey = Convert.ToBase64String(passkey.PublicKey),
                name = passkey.Name,
                createdAt = passkey.CreatedAt,
                signCount = passkey.SignCount,
                transports = passkey.Transports,
                isUserVerified = passkey.IsUserVerified,
                isBackupEligible = passkey.IsBackupEligible,
                isBackedUp = passkey.IsBackedUp,
                attestationObject = Convert.ToBase64String(passkey.AttestationObject),
                clientDataJson = Convert.ToBase64String(passkey.ClientDataJson),
            }
        );

    private static UserPasskeyInfo DeserializePasskeyFromJson(byte[] credentialId, string jsonData)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(jsonData);
        var root = doc.RootElement;
        return new UserPasskeyInfo(
            credentialId: credentialId,
            publicKey: Convert.FromBase64String(root.GetProperty("publicKey").GetString()!),
            createdAt: root.GetProperty("createdAt").GetDateTimeOffset(),
            signCount: (uint)root.GetProperty("signCount").GetUInt64(),
            transports: root.TryGetProperty("transports", out var transportsElement)
            && transportsElement.ValueKind != System.Text.Json.JsonValueKind.Null
                ? transportsElement.EnumerateArray().Select(e => e.GetString()!).ToArray()
                : null,
            isUserVerified: root.GetProperty("isUserVerified").GetBoolean(),
            isBackupEligible: root.GetProperty("isBackupEligible").GetBoolean(),
            isBackedUp: root.GetProperty("isBackedUp").GetBoolean(),
            attestationObject: Convert.FromBase64String(root.GetProperty("attestationObject").GetString()!),
            clientDataJson: Convert.FromBase64String(root.GetProperty("clientDataJson").GetString()!)
        )
        {
            Name =
                root.TryGetProperty("name", out var nameElement)
                && nameElement.ValueKind != System.Text.Json.JsonValueKind.Null
                    ? nameElement.GetString()
                    : null,
        };
    }
}
