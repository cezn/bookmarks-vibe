namespace BookmarksApi.Shared;

public record Cursor(string SortValue, int Id)
{
    public static bool TryParse(string? base64, out Cursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrEmpty(base64))
            return false;
        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var parts = decoded.Split('|');
            if (parts.Length != 2)
                return false;
            cursor = new Cursor(parts[0], int.Parse(parts[1]));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string ToBase64()
    {
        var raw = $"{SortValue}|{Id}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }
}
