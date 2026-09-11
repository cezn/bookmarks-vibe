using System.Reflection;
using BookmarksApi.Idempotency;

namespace BookmarksApi.Shared;

public record UserId(string Value) : IBindableFromHttpContext<UserId>
{
    public static ValueTask<UserId?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var userId = context.RequestServices.GetRequiredService<UserIdProvider>().FindUserId(context);
        return ValueTask.FromResult(string.IsNullOrWhiteSpace(userId) ? null : new UserId(userId));
    }

    public static implicit operator string(UserId userId) => userId.Value;
}
