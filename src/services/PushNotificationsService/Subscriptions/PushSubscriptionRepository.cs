using Npgsql;
using PushNotificationsService.Shared;

namespace PushNotificationsService.Subscriptions;

public static class PushSubscriptionRepositoryExtensions
{
    public static async Task<PushSubscription?> GetSubscriptionByIdAsync(
        this NpgsqlConnection con,
        int id,
        string userId
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "SELECT id, user_id, device_token, platform, endpoint, auth, p256dh, created_at, updated_at, is_active FROM push_subscriptions WHERE id = $1 AND user_id = $2";
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(userId);
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("SelectSubscriptionById");
        if (await reader.ReadAsync())
        {
            return new PushSubscription(
                Id: reader.GetInt32(0),
                UserId: reader.GetString(1),
                DeviceToken: reader.GetString(2),
                Platform: reader.GetString(3),
                Endpoint: reader.IsDBNull(4) ? null : reader.GetString(4),
                Auth: reader.IsDBNull(5) ? null : reader.GetString(5),
                P256dh: reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedAt: reader.GetFieldValue<DateTimeOffset>(7),
                UpdatedAt: reader.GetFieldValue<DateTimeOffset>(8),
                IsActive: reader.GetBoolean(9)
            );
        }
        return null;
    }

    public static async Task<IEnumerable<PushSubscription>> GetSubscriptionsByUserIdAsync(
        this NpgsqlConnection con,
        string userId
    )
    {
        var subscriptions = new List<PushSubscription>();
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "SELECT id, user_id, device_token, platform, endpoint, auth, p256dh, created_at, updated_at, is_active FROM push_subscriptions WHERE user_id = $1 AND is_active = true ORDER BY created_at DESC";
        cmd.Parameters.AddWithValue(userId);
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("SelectSubscriptionsByUserId");
        while (await reader.ReadAsync())
        {
            subscriptions.Add(
                new PushSubscription(
                    Id: reader.GetInt32(0),
                    UserId: reader.GetString(1),
                    DeviceToken: reader.GetString(2),
                    Platform: reader.GetString(3),
                    Endpoint: reader.IsDBNull(4) ? null : reader.GetString(4),
                    Auth: reader.IsDBNull(5) ? null : reader.GetString(5),
                    P256dh: reader.IsDBNull(6) ? null : reader.GetString(6),
                    CreatedAt: reader.GetFieldValue<DateTimeOffset>(7),
                    UpdatedAt: reader.GetFieldValue<DateTimeOffset>(8),
                    IsActive: reader.GetBoolean(9)
                )
            );
        }
        return subscriptions;
    }

    public static async Task<PushSubscription> CreateSubscriptionAsync(
        this NpgsqlConnection con,
        string userId,
        CreateSubscriptionRequest request
    )
    {
        var now = DateTimeOffset.UtcNow;
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "INSERT INTO push_subscriptions (user_id, device_token, platform, endpoint, auth, p256dh, created_at, updated_at, is_active) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9) RETURNING id, user_id, device_token, platform, endpoint, auth, p256dh, created_at, updated_at, is_active";
        cmd.Parameters.AddWithValue(userId);
        cmd.Parameters.AddWithValue(request.DeviceToken);
        cmd.Parameters.AddWithValue(request.Platform);
        cmd.Parameters.AddWithValue(request.Endpoint ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue(request.Auth ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue(request.P256dh ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue(now);
        cmd.Parameters.AddWithValue(now);
        cmd.Parameters.AddWithValue(true);

        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("CreateSubscription");
        if (await reader.ReadAsync())
        {
            return new PushSubscription(
                Id: reader.GetInt32(0),
                UserId: reader.GetString(1),
                DeviceToken: reader.GetString(2),
                Platform: reader.GetString(3),
                Endpoint: reader.IsDBNull(4) ? null : reader.GetString(4),
                Auth: reader.IsDBNull(5) ? null : reader.GetString(5),
                P256dh: reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedAt: reader.GetFieldValue<DateTimeOffset>(7),
                UpdatedAt: reader.GetFieldValue<DateTimeOffset>(8),
                IsActive: reader.GetBoolean(9)
            );
        }
        throw new InvalidOperationException("Failed to create subscription");
    }

    public static async Task<bool> DeleteSubscriptionAsync(this NpgsqlConnection con, int id, string userId)
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "UPDATE push_subscriptions SET is_active = false, updated_at = $1 WHERE id = $2 AND user_id = $3";
        cmd.Parameters.AddWithValue(DateTimeOffset.UtcNow);
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(userId);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("DeleteSubscription") > 0;
    }

    public static async Task<int> ExecuteNonQueryWithSpanNameAsync(this NpgsqlCommand cmd, string spanName)
    {
        return await cmd.ExecuteNonQueryAsync();
    }
}
