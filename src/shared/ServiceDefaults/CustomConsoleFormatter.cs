using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace ServiceDefaults;

public static class CustomConsoleFormatterExtensions
{
    public static ILoggingBuilder AddCustomConsoleFormatter(this ILoggingBuilder builder)
    {
        builder
            .AddConsole(options =>
            {
                options.FormatterName = "custom";
            })
            .AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>();

        return builder;
    }
}

public sealed class CustomConsoleFormatterOptions : ConsoleFormatterOptions
{
    public string? LoggerNameColor { get; set; } = "\x1b[36m"; // Cyan by default
    public bool IncludeTimestamp { get; set; } = false;
}

public sealed class CustomConsoleFormatter(IOptionsMonitor<CustomConsoleFormatterOptions> options)
    : ConsoleFormatter("custom")
{
    // Disable formating for test projects during 'dotnet run'. When run with VSCode output is not redirected.
    private static readonly bool DisableColors =
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Test" && !Console.IsOutputRedirected;

    private static readonly string[] Colors =
    [
        "\x1b[31m", // Red
        "\x1b[32m", // Green
        "\x1b[33m", // Yellow
        "\x1b[34m", // Blue
        "\x1b[35m", // Magenta
        "\x1b[36m", // Cyan
        "\x1b[91m", // Bright red
        "\x1b[92m", // Bright green
        "\x1b[93m", // Bright yellow
        "\x1b[94m", // Bright blue
        "\x1b[95m", // Bright magenta
        "\x1b[96m", // Bright cyan
        "\x1b[38;5;39m", // Dodger Blue
        "\x1b[90m", // Dark gray
        "\x1b[38;5;39m", // Dodger Blue
        "\x1b[30m", // Black
        "\x1b[38;5;208m", // Orange
        "\x1b[38;5;27m", // Deep blue
        "\x1b[38;5;129m", // Violet
        "\x1b[38;5;202m", // OrangeRed
        "\x1b[38;5;46m", // Spring green
        "\x1b[38;5;226m", // Bright yellow
        "\x1b[38;5;51m", // Aqua
        "\x1b[38;5;201m", // Pink
    ];

    // new: track last log time in UTC ticks (thread-safe via Interlocked)
    private static long s_lastLogTicks;

    private readonly CustomConsoleFormatterOptions _options = options.CurrentValue;

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter
    )
    {
        var logLevel = logEntry.LogLevel;
        var categoryName = logEntry.Category;
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        var eventId = logEntry.EventId;
        var exception = logEntry.Exception;

        if (_options.IncludeTimestamp)
            textWriter.Write($"{DateTime.Now:HH:mm:ss.fff} ");

        textWriter.Write($"{FormatLogLevel(logLevel)}: ");
        textWriter.Write($"{FormatCategoryName(categoryName)}[{eventId.Id}]");
        textWriter.Write($"(T{Environment.CurrentManagedThreadId})");
        textWriter.Write($"{FormatDuration()}\n");
        textWriter.WriteLine($"        {message}");

        if (exception != null)
            textWriter.WriteLine(exception.ToString());
    }

    private static string FormatDuration()
    {
        // compute duration since last log (UTC) in a thread-safe way
        var nowUtc = DateTime.UtcNow;
        long previousTicks = Interlocked.Exchange(ref s_lastLogTicks, nowUtc.Ticks);

        if (previousTicks == 0)
            return "";

        var prev = new DateTime(previousTicks, DateTimeKind.Utc);
        var duration = nowUtc - prev;
        // format duration: ms if < 1s otherwise s
        string durationValue =
            duration.TotalMilliseconds < 1000
                ? $"{duration.TotalMilliseconds:0.###}ms"
                : $"{duration.TotalSeconds:0.###}s";

        if (DisableColors)
            return $"+{durationValue} ";

        const string durationColor = "\x1b[32m"; // green
        const string resetColor = "\x1b[0m";

        return $"{durationColor}+{durationValue}{resetColor} ";
    }

    private static string FormatLogLevel(LogLevel logLevel)
    {
        // Map log level to 4-letter abbreviation without colors
        string logLevelAbbr = logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => "none",
        };

        if (DisableColors)
        {
            return $"[{logLevelAbbr}]";
        }

        // Reset color
        const string resetColor = "\x1b[0m";

        // Log level colors
        var logLevelColor = logLevel switch
        {
            LogLevel.Critical => "\x1b[91m", // Bright red
            LogLevel.Error => "\x1b[31m", // Red
            LogLevel.Warning => "\x1b[33m", // Yellow
            LogLevel.Information => "\x1b[32m", // Green
            LogLevel.Debug => "\x1b[37m", // White
            LogLevel.Trace => "\x1b[90m", // Dark gray
            _ => resetColor,
        };

        return $"{logLevelColor}[{logLevelAbbr}]{resetColor}";
    }

    private static string FormatCategoryName(string categoryName)
    {
        if (DisableColors)
            return categoryName;

        const string resetColor = "\x1b[0m";
        var segments = categoryName.Split('.');
        var coloredSegments = segments.Select(segment =>
        {
            // Hash the segment to pick a color deterministically
            // Use a stable hash algorithm (e.g., FNV-1a) for consistent color assignment
            unchecked
            {
                const int fnvPrime = 16777619;
                int hash = (int)2166136261;
                foreach (char c in segment)
                {
                    hash ^= c;
                    hash *= fnvPrime;
                }
                int colorIndex = Math.Abs(hash) % Colors.Length;
                return $"{Colors[colorIndex]}{segment}{resetColor}";
            }
        });
        return string.Join(".", coloredSegments);
    }
}
