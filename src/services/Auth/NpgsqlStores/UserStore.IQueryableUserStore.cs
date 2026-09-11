using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Auth.NpgsqlStores;

public partial class UserStore
{
    public IQueryable<ApplicationUser> Users => GetAllUsers().ToList().AsQueryable();

    public IEnumerable<ApplicationUser> GetAllUsers()
    {
        using var connection = db.OpenConnectionTrace();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, user_name, normalized_user_name, email, normalized_email, email_confirmed,
                password_hash, security_stamp, concurrency_stamp, phone_number, phone_number_confirmed,
                two_factor_enabled, lockout_end, lockout_enabled, access_failed_count, authenticator_key
            FROM aspnet_users
            """;
        using var reader = command.ExecuteReaderWithSpanName("GetAllUsers");
        while (reader.Read())
            yield return MapUserFromReader(reader);
    }

    private static ApplicationUser MapUserFromReader(NpgsqlDataReader reader) =>
        new ApplicationUser
        {
            Id = reader.GetString(0),
            UserName = reader.IsDBNull(1) ? null : reader.GetString(1),
            NormalizedUserName = reader.IsDBNull(2) ? null : reader.GetString(2),
            Email = reader.IsDBNull(3) ? null : reader.GetString(3),
            NormalizedEmail = reader.IsDBNull(4) ? null : reader.GetString(4),
            EmailConfirmed = reader.GetBoolean(5),
            PasswordHash = reader.IsDBNull(6) ? null : reader.GetString(6),
            SecurityStamp = reader.IsDBNull(7) ? null : reader.GetString(7),
            ConcurrencyStamp = reader.IsDBNull(8) ? null : reader.GetString(8),
            PhoneNumber = reader.IsDBNull(9) ? null : reader.GetString(9),
            PhoneNumberConfirmed = reader.GetBoolean(10),
            TwoFactorEnabled = reader.GetBoolean(11),
            LockoutEnd = reader.IsDBNull(12) ? null : reader.GetFieldValue<DateTimeOffset>(12),
            LockoutEnabled = reader.GetBoolean(13),
            AccessFailedCount = reader.GetInt32(14),
            AuthenticatorKey = reader.IsDBNull(15) ? null : reader.GetString(15),
        };
}
