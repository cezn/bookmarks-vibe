using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Auth.NpgsqlStores;

public partial class RoleStore : IRoleClaimStore<ApplicationRole>
{
    public async Task<IList<Claim>> GetClaimsAsync(ApplicationRole role, CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>();

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT claim_type, claim_value
            FROM aspnet_role_claims
            WHERE role_id = $1
            """;
        command.Parameters.AddWithValue(role.Id ?? "");
        using var reader = await command.ExecuteReaderWithSpanNameAsync("GetRoleClaims");
        while (await reader.ReadAsync(cancellationToken))
            claims.Add(new Claim(reader.GetString(0), reader.GetString(1)));

        return claims;
    }

    public async Task AddClaimAsync(ApplicationRole role, Claim claim, CancellationToken cancellationToken = default)
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO aspnet_role_claims (role_id, claim_type, claim_value)
            VALUES ($1, $2, $3)
            """;
        command.Parameters.AddWithValue(role.Id ?? "");
        command.Parameters.AddWithValue(claim.Type ?? "");
        command.Parameters.AddWithValue(claim.Value ?? "");
        await command.ExecuteNonQueryWithSpanNameAsync("AddRoleClaim");
    }

    public async Task RemoveClaimAsync(ApplicationRole role, Claim claim, CancellationToken cancellationToken = default)
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM aspnet_role_claims
            WHERE role_id = $1 AND claim_type = $2 AND claim_value = $3
            """;
        command.Parameters.AddWithValue(role.Id ?? "");
        command.Parameters.AddWithValue(claim.Type ?? "");
        command.Parameters.AddWithValue(claim.Value ?? "");
        await command.ExecuteNonQueryWithSpanNameAsync("RemoveRoleClaim");
    }
}
