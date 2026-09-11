using System.Text.Json;
using BookmarksApi.Shared;
using Npgsql;

namespace BookmarksApi.Idempotency;

public static class IdempotencyDbConnectionExtensions
{
    public record IdempotencyResponse(int StatusCode, Dictionary<string, string> Headers, byte[] Body);

    public record IdempotencyCheckResult(IdempotencyResponse? ExistingResponse, bool IsInProgress, bool IsNewEntry);

    public static async Task<IdempotencyCheckResult> GetOrInsertIdempotencyKeyAsync(
        this NpgsqlConnection con,
        string idempotencyKey,
        string userId,
        string operationName,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            INSERT INTO idempotency_keys (idempotency_key, user_id, operation_name, created_at, last_accessed_at, status)
            VALUES ($1, $2, $3, $4, $4, 'in-progress')
            ON CONFLICT (idempotency_key, user_id, operation_name) DO UPDATE SET last_accessed_at = $4
            RETURNING xmax = 0 as was_inserted, status, response_status_code, response_headers, response_body
            """;
        cmd.Parameters.AddWithValue(idempotencyKey);
        cmd.Parameters.AddWithValue(userId);
        cmd.Parameters.AddWithValue(operationName);
        cmd.Parameters.AddWithValue(DateTimeOffset.UtcNow);

        using var reader = await cmd.ExecuteReaderWithSpanNameAsync("EnsureIdempotencyKey", ct);
        if (await reader.ReadAsync(ct))
        {
            var wasInserted = reader.GetBoolean(0);
            var status = reader.GetString(1);

            // If it was inserted, it's a new entry
            if (wasInserted)
                return new IdempotencyCheckResult(null, false, true);

            // If it already existed, check its status
            if (status == "in-progress")
                return new IdempotencyCheckResult(null, true, false);

            // If it's completed, return the existing response
            var statusCode = reader.GetInt32(2);
            var headersJson = reader.IsDBNull(3) ? "{}" : reader.GetString(3);
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson) ?? [];
            var body = reader.IsDBNull(4) ? [] : (byte[])reader.GetValue(4);
            return new IdempotencyCheckResult(new IdempotencyResponse(statusCode, headers, body), false, false);
        }

        return new IdempotencyCheckResult(null, false, false);
    }

    public static async Task StoreIdempotencyResponseAsync(
        this NpgsqlConnection con,
        string idempotencyKey,
        string userId,
        string operationName,
        int statusCode,
        Dictionary<string, string> headers,
        byte[] body,
        CancellationToken ct = default
    )
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            UPDATE idempotency_keys
            SET response_status_code = $1, response_headers = $2::jsonb, response_body = $3, status = 'completed'
            WHERE idempotency_key = $4 AND user_id = $5 AND operation_name = $6
            """;
        cmd.Parameters.AddWithValue(statusCode);
        cmd.Parameters.AddWithValue(NpgsqlTypes.NpgsqlDbType.Text, JsonSerializer.Serialize(headers));
        cmd.Parameters.AddWithValue(body);
        cmd.Parameters.AddWithValue(idempotencyKey);
        cmd.Parameters.AddWithValue(userId);
        cmd.Parameters.AddWithValue(operationName);

        await cmd.ExecuteNonQueryWithSpanNameAsync("StoreIdempotencyKey", ct);
    }
}
