namespace BookmarksApi.Shared;

public record CaseInsensitive<T>(T Value)
    where T : struct, Enum
{
    public static bool TryParse(string? value, out CaseInsensitive<T>? val)
    {
        if (Enum.TryParse<T>(value, ignoreCase: true, out var enumVal))
        {
            val = new CaseInsensitive<T>(enumVal);
            return true;
        }

        val = null;
        return false;
    }
}
