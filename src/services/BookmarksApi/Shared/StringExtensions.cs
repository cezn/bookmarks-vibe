namespace BookmarksApi.Shared;

public static class StringExtensions
{
    /// <summary>
    /// Truncate a string to a maximum length and append "..." if truncated.
    /// Uses the same behavior as the original inlined code (max length = 200).
    /// </summary>
    public static string TruncateForAttribute(this string? s, int maxLen = 200)
    {
        if (string.IsNullOrEmpty(s))
            return s ?? string.Empty;
        return s.Length > maxLen ? s[..maxLen] + "..." : s;
    }
}
