namespace BookmarksApi.Idempotency;

/// <summary>
/// Marks an endpoint as requiring idempotency handling.
/// The endpoint must accept an X-Idempotency-Key header to enable idempotency.
/// When applied, the middleware will:
/// - Cache the response based on the idempotency key and user ID
/// - Return cached responses for duplicate requests
/// - Add X-Idempotency-Hit header to indicate cache hits
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class IdempotentAttribute : Attribute { }
