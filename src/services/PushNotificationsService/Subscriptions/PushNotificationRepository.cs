using Npgsql;
using PushNotificationsService.Shared;

namespace PushNotificationsService.Subscriptions;

public static class PushNotificationRepositoryExtensions
{
    public static async Task<PushNotification> CreateNotificationAsync(
        this NpgsqlConnection con,
        string userId,
        SendNotificationRequest request
    )
    {
        var now = DateTimeOffset.UtcNow;
        var dataJson = request.Data != null ? System.Text.Json.JsonSerializer.Serialize(request.Data) : null;

        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "INSERT INTO push_notifications (user_id, title, body, data, created_at, sent) VALUES ($1, $2, $3, $4, $5, $6) RETURNING id, user_id, title, body, data, created_at, sent";
        cmd.Parameters.AddWithValue(userId);
        cmd.Parameters.AddWithValue(request.Title);
        cmd.Parameters.AddWithValue(request.Body);
        cmd.Parameters.AddWithValue(dataJson ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue(now);
        cmd.Parameters.AddWithValue(false);

        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("CreateNotification");
        if (await reader.ReadAsync())
        {
            var dataString = reader.IsDBNull(4) ? null : reader.GetString(4);
            var data =
                dataString != null
                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(dataString)
                    : null;

            return new PushNotification(
                Id: reader.GetInt32(0),
                UserId: reader.GetString(1),
                Title: reader.GetString(2),
                Body: reader.GetString(3),
                Data: data,
                CreatedAt: reader.GetFieldValue<DateTimeOffset>(5),
                Sent: reader.GetBoolean(6)
            );
        }
        throw new InvalidOperationException("Failed to create notification");
    }

    public static async Task<bool> MarkNotificationAsSentAsync(this NpgsqlConnection con, int notificationId)
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE push_notifications SET sent = true WHERE id = $1";
        cmd.Parameters.AddWithValue(notificationId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public static async Task<PushNotification?> GetNotificationByIdAsync(this NpgsqlConnection con, int id)
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "SELECT id, user_id, title, body, data, created_at, sent FROM push_notifications WHERE id = $1";
        cmd.Parameters.AddWithValue(id);
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("SelectNotificationById");
        if (await reader.ReadAsync())
        {
            var dataString = reader.IsDBNull(4) ? null : reader.GetString(4);
            var data =
                dataString != null
                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(dataString)
                    : null;

            return new PushNotification(
                Id: reader.GetInt32(0),
                UserId: reader.GetString(1),
                Title: reader.GetString(2),
                Body: reader.GetString(3),
                Data: data,
                CreatedAt: reader.GetFieldValue<DateTimeOffset>(5),
                Sent: reader.GetBoolean(6)
            );
        }
        return null;
    }
}
