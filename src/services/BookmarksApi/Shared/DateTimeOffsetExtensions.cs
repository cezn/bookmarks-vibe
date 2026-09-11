namespace BookmarksApi.Shared;

public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset TruncateToSeconds(this DateTimeOffset self) =>
        self.AddTicks(-(self.Ticks % TimeSpan.TicksPerSecond));
}
