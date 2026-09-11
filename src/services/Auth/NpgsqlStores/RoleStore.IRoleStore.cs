using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Auth.NpgsqlStores;

public partial class RoleStore : IRoleStore<ApplicationRole>
{
    public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        string sql = """
            INSERT INTO aspnet_roles (id, name, normalized_name, concurrency_stamp)
            VALUES ($1, $2, $3, $4)
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue(role.Id ?? "");
        command.Parameters.AddWithValue(role.Name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(role.NormalizedName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(role.ConcurrencyStamp ?? Guid.NewGuid().ToString());

        await command.ExecuteNonQueryWithSpanNameAsync("CreateRole");

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        string sql = """
            UPDATE aspnet_roles
            SET name = $2,
                normalized_name = $3,
                concurrency_stamp = $4
            WHERE id = $1
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue(role.Id ?? "");
        command.Parameters.AddWithValue(role.Name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(role.NormalizedName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(role.ConcurrencyStamp ?? Guid.NewGuid().ToString());

        await command.ExecuteNonQueryWithSpanNameAsync("UpdateRole");

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        string sql = "DELETE FROM \"aspnet_roles\" WHERE id = $1";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue(role.Id ?? "");
        await command.ExecuteNonQueryWithSpanNameAsync("DeleteRole");

        return IdentityResult.Success;
    }

    public async Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken) => role.Id;

    public async Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken) => role.Name;

    public async Task SetRoleNameAsync(ApplicationRole role, string? name, CancellationToken cancellationToken) =>
        role.Name = name;

    public async Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken) =>
        role.NormalizedName;

    public async Task SetNormalizedRoleNameAsync(
        ApplicationRole role,
        string? normalizedName,
        CancellationToken cancellationToken
    ) => role.NormalizedName = normalizedName;

    public async Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roleId))
            return null;

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        string sql = """
            SELECT id, name, normalized_name, concurrency_stamp
            FROM aspnet_roles
            WHERE id = $1
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue(roleId);
        using var reader = await command.ExecuteReaderWithSpanNameAsync("FindRoleById");
        if (await reader.ReadAsync(cancellationToken))
            return MapRoleFromReader(reader);

        return null;
    }

    public async Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedRoleName))
            return null;

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        string sql = """
            SELECT id, name, normalized_name, concurrency_stamp
            FROM aspnet_roles
            WHERE normalized_name = $1
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue(normalizedRoleName);
        using var reader = await command.ExecuteReaderWithSpanNameAsync("FindRoleByName");

        return await reader.ReadAsync(cancellationToken) ? MapRoleFromReader(reader) : null;
    }
}
