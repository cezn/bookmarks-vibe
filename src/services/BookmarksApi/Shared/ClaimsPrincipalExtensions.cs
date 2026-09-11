using System.Security.Claims;

namespace BookmarksApi.Shared;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("User identifier not found in claims");
}
