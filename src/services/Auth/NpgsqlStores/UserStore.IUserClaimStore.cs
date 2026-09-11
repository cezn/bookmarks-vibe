using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Auth.NpgsqlStores;

public partial class UserStore
{
    public async Task<IList<Claim>> GetClaimsAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var claims = new List<Claim>();

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT claim_type, claim_value
            FROM aspnet_user_claims
            WHERE user_id = $1
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        using var reader = await command.ExecuteReaderWithSpanNameAsync("GetUserClaims");
        while (await reader.ReadAsync(cancellationToken))
            claims.Add(new Claim(reader.GetString(0), reader.GetString(1)));

        return claims;
    }

    public async Task AddClaimsAsync(
        ApplicationUser user,
        IEnumerable<Claim> claims,
        CancellationToken cancellationToken
    )
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        foreach (var claim in claims)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO aspnet_user_claims (user_id, claim_type, claim_value)
                VALUES ($1, $2, $3)
                """;
            command.Parameters.AddWithValue(user.Id ?? "");
            command.Parameters.AddWithValue(claim.Type ?? "");
            command.Parameters.AddWithValue(claim.Value ?? "");
            await command.ExecuteNonQueryWithSpanNameAsync("AddUserClaim");
        }
    }

    public async Task RemoveClaimsAsync(
        ApplicationUser user,
        IEnumerable<Claim> claims,
        CancellationToken cancellationToken
    )
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        foreach (var claim in claims)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                DELETE FROM aspnet_user_claims
                WHERE user_id = $1 AND claim_type = $2 AND claim_value = $3
                """;
            command.Parameters.AddWithValue(user.Id ?? "");
            command.Parameters.AddWithValue(claim.Type ?? "");
            command.Parameters.AddWithValue(claim.Value ?? "");
            await command.ExecuteNonQueryWithSpanNameAsync("RemoveUserClaim");
        }
    }

    public async Task<IList<ApplicationUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
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
            INNER JOIN aspnet_user_claims uc ON u.id = uc.user_id
            WHERE uc.claim_type = $1 AND uc.claim_value = $2
            """;

        command.Parameters.AddWithValue(claim.Type ?? "");
        command.Parameters.AddWithValue(claim.Value ?? "");
        using var reader = await command.ExecuteReaderWithSpanNameAsync("GetUsersForClaim");
        while (await reader.ReadAsync(cancellationToken))
            users.Add(MapUserFromReader(reader));

        return users;
    }

    public async Task ReplaceClaimAsync(
        ApplicationUser user,
        Claim claim,
        Claim newClaim,
        CancellationToken cancellationToken
    )
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE aspnet_user_claims
            SET claim_type = $2, claim_value = $3
            WHERE user_id = $1 AND claim_type = $4 AND claim_value = $5
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(newClaim.Type ?? "");
        command.Parameters.AddWithValue(newClaim.Value ?? "");
        command.Parameters.AddWithValue(claim.Type ?? "");
        command.Parameters.AddWithValue(claim.Value ?? "");
        await command.ExecuteNonQueryWithSpanNameAsync("ReplaceUserClaim");
    }
}
