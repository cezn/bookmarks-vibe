using System.Diagnostics;
using System.Text;
using BookmarksApi.Shared;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using ServiceDefaults;

namespace BookmarksApi.Outbox;

public static class OutboxDbConnectionExtensions
{
    public static async Task<IEnumerable<OutboxMessage>> GetAllOutboxMessagesAsync(
        this NpgsqlConnection con,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT id, aggregatetype, aggregateid, user_id, type, payload, created_at FROM outbox ORDER BY id";
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("SelectAllOutboxMessages", ct);
        var messages = new List<OutboxMessage>();
        while (await reader.ReadAsync(ct))
        {
            messages.Add(
                new OutboxMessage(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.IsDBNull(5) ? Array.Empty<byte>() : (byte[])reader["payload"],
                    reader.GetDateTime(6)
                )
            );
        }
        return messages;
    }

    public static async Task<OutboxMessage?> GetOutboxMessageByIdAsync(
        this NpgsqlConnection con,
        Guid id,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT id, aggregatetype, aggregateid, user_id, type, payload, created_at FROM outbox WHERE id = $1";
        cmd.Parameters.AddWithValue(id);
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("SelectOutboxMessageById", ct);

        return await reader.ReadAsync(ct)
            ? new OutboxMessage(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? Array.Empty<byte>() : (byte[])reader["payload"],
                reader.GetDateTime(6)
            )
            : null;
    }

    public static async Task<OutboxMessage> AddOutboxMessageAsync(
        this NpgsqlConnection con,
        OutboxMessage message,
        CancellationToken ct = default
    )
    {
        using var activity = Monitoring.ActivitySource.StartActivity("AddOutboxMessage", ActivityKind.Internal);
        activity?.SetTag("aggregateType", message.AggregateType);
        activity?.SetTag("aggregateId", message.AggregateId);
        activity?.SetTag("type", message.Type);

        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "INSERT INTO outbox (id, aggregatetype, aggregateid, user_id, type, payload, tracingspancontext) VALUES ($1, $2, $3, $4, $5, $6, $7) RETURNING id";
        cmd.Parameters.AddWithValue(message.Id);
        cmd.Parameters.AddWithValue(message.AggregateType);
        cmd.Parameters.AddWithValue(message.AggregateId);
        cmd.Parameters.AddWithValue(message.UserId);
        cmd.Parameters.AddWithValue(message.Type);
        cmd.Parameters.AddWithValue(NpgsqlTypes.NpgsqlDbType.Bytea, message.Payload ?? Array.Empty<byte>());
        // Add tracing span context as serialized java.util.Properties
        var spanContext = GetSerializedSpanContext();
        cmd.Parameters.AddWithValue(spanContext ?? (object)DBNull.Value);
        var id = (Guid)(await cmd.ExecuteScalarWithSpanNameAsync("InsertOutboxMessage", ct))!;
        return message with { Id = id };
    }

    public static async Task<bool> RemoveOutboxMessageAsync(
        this NpgsqlConnection con,
        Guid id,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM outbox WHERE id = $1";
        cmd.Parameters.AddWithValue(id);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("DeleteOutboxMessage", ct) > 0;
    }

    public static async Task<bool> UpdateOutboxMessageAsync(
        this NpgsqlConnection con,
        OutboxMessage updatedMessage,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "UPDATE outbox SET aggregatetype = $1, aggregateid = $2, user_id = $3, type = $4, payload = $5, tracingspancontext = $6 WHERE id = $7";
        cmd.Parameters.AddWithValue(updatedMessage.AggregateType);
        cmd.Parameters.AddWithValue(updatedMessage.AggregateId);
        cmd.Parameters.AddWithValue(updatedMessage.UserId);
        cmd.Parameters.AddWithValue(updatedMessage.Type);
        cmd.Parameters.AddWithValue(NpgsqlTypes.NpgsqlDbType.Bytea, updatedMessage.Payload ?? Array.Empty<byte>());
        // Update tracing span context as serialized java.util.Properties
        var spanContext = GetSerializedSpanContext();
        cmd.Parameters.AddWithValue(spanContext ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue(updatedMessage.Id);
        return await cmd.ExecuteNonQueryWithSpanNameAsync("UpdateOutboxMessage", ct) > 0;
    }

    private static string? GetSerializedSpanContext()
    {
        var activity = Activity.Current;
        if (activity == null)
            return null;
        var propagator = Propagators.DefaultTextMapPropagator;
        var dict = new Dictionary<string, string>();
        propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), dict, (d, k, v) => d[k] = v);
        if (dict.Count == 0)
            return null;
        var sb = new StringBuilder();
        foreach (var kvp in dict)
        {
            sb.Append(kvp.Key).Append('=').Append(kvp.Value.Replace("\n", "\\n")).Append('\n');
        }
        return sb.ToString();
    }

    public static async Task<IEnumerable<OutboxMessage>> GetOutboxMessagesByAggregateIdAsync(
        this NpgsqlConnection con,
        string aggregateId,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText =
            "SELECT id, aggregatetype, aggregateid, user_id, type, payload, created_at FROM outbox WHERE aggregateid = $1 ORDER BY id";
        cmd.Parameters.AddWithValue(aggregateId);
        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("SelectOutboxMessagesByAggregateId", ct);
        var messages = new List<OutboxMessage>();
        while (await reader.ReadAsync(ct))
        {
            messages.Add(
                new OutboxMessage(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.IsDBNull(5) ? Array.Empty<byte>() : (byte[])reader["payload"],
                    reader.GetDateTime(6)
                )
            );
        }
        return messages;
    }
}
