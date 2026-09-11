using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Auth.NpgsqlStores;

public class ApplicationRole
{
    public string Id { get; set; } = "";
    public string? Name { get; set; }
    public string? NormalizedName { get; set; }
    public string? ConcurrencyStamp { get; set; }
}

public partial class RoleStore(NpgsqlDataSource db)
{
    public void Dispose()
    {
        // Npgsql connections are managed by using statements, nothing to dispose here
    }

    private static ApplicationRole MapRoleFromReader(NpgsqlDataReader reader) =>
        new ApplicationRole
        {
            Id = reader.GetString(0),
            Name = reader.IsDBNull(1) ? null : reader.GetString(1),
            NormalizedName = reader.IsDBNull(2) ? null : reader.GetString(2),
            ConcurrencyStamp = reader.IsDBNull(3) ? null : reader.GetString(3),
        };

    public IEnumerable<ApplicationRole> GetAllRoles()
    {
        using var connection = db.OpenConnectionTrace();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, normalized_name, concurrency_stamp
            FROM aspnet_roles
            """;
        using var reader = command.ExecuteReaderWithSpanName("GetAllRoles");
        while (reader.Read())
            yield return MapRoleFromReader(reader);
    }
}
