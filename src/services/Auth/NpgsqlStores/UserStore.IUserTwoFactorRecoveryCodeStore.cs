using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Auth.NpgsqlStores;

public partial class UserStore
{
    public async Task ReplaceCodesAsync(
        ApplicationUser user,
        IEnumerable<string> recoveryCodes,
        CancellationToken cancellationToken
    )
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        // First, delete all existing recovery codes for this user
        string deleteSql = """
            DELETE FROM aspnet_user_recovery_codes
            WHERE user_id = $1
            """;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = deleteSql;
            command.Parameters.AddWithValue(user.Id ?? "");
            await command.ExecuteNonQueryWithSpanNameAsync("DeleteUserRecoveryCodes");
        }

        // Then insert the new recovery codes
        foreach (var code in recoveryCodes)
        {
            string insertSql = """
                INSERT INTO aspnet_user_recovery_codes (user_id, code, redeemed, created_at)
                VALUES ($1, $2, false, CURRENT_TIMESTAMP)
                """;

            using var command = connection.CreateCommand();
            command.CommandText = insertSql;
            command.Parameters.AddWithValue(user.Id ?? "");
            command.Parameters.AddWithValue(code ?? "");
            await command.ExecuteNonQueryWithSpanNameAsync("InsertRecoveryCode");
        }
    }

    public async Task<bool> RedeemCodeAsync(ApplicationUser user, string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE aspnet_user_recovery_codes
            SET redeemed = true
            WHERE user_id = $1 AND code = $2 AND redeemed = false
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        command.Parameters.AddWithValue(code);
        var result = await command.ExecuteNonQueryWithSpanNameAsync("RedeemRecoveryCode");
        return result > 0;
    }

    public async Task<int> CountCodesAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        using var connection = await db.OpenConnectionTraceAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM aspnet_user_recovery_codes
            WHERE user_id = $1 AND redeemed = false
            """;
        command.Parameters.AddWithValue(user.Id ?? "");
        var result = await command.ExecuteScalarWithSpanNameAsync("CountUserRecoveryCodes");
        return Convert.ToInt32(result ?? 0);
    }
}
