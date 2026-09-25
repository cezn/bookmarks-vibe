using System.Diagnostics;
using System.Runtime.CompilerServices;
using BookmarksApi.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BookmarksApi.Tests;

/// <summary>
/// Upon creation, this class:
/// - Generates a UserId.
/// - Creates a WebApplicationFactory.
/// - Uses the app's DI to set up the connection, authenticated HTTP clients, and logger.
/// - Creates an Activity instance.
/// - Provides helper methods to create entity builders.
/// </summary>
public class TestFixture : IDisposable
{
    public static readonly ActivitySource ActivitySource = new("BookmarksApi.Tests");

    private readonly Activity? _activity;

    public Guid UserId { get; private set; }
    public NpgsqlConnection Connection { get; private set; } = null!;
    public CustomWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient HttpClientJwt { get; private set; } = null!;
    public HttpClient HttpClientApiKey { get; private set; } = null!;
    public ILogger Logger { get; private set; }

    static TestFixture()
    {
        SetEnvironmentVariables();
    }

    public TestFixture(
        [CallerMemberName] string? caller = null,
        [CallerFilePath] string? filePath = null,
        Guid? userId = null
    )
    {
        caller ??= "UnknownCaller";
        filePath ??= "UnknownFilePath";
        var className = Path.GetFileNameWithoutExtension(filePath);

        UserId = userId ?? Guid.CreateVersion7();
        Factory = new CustomWebApplicationFactory();
        Connection = Factory.Services.GetRequiredKeyedService<NpgsqlConnection>("rw");
        _activity = ActivitySource.StartActivity(ActivityKind.Internal, name: $"{className}:{caller}");
        _activity?.AddTag("userid", UserId.ToString());
        Connection.Open();
        HttpClientJwt = CreateAuthenticatedJwtClient();
        HttpClientApiKey = CreateAuthenticatedApiKeyClient();
        Logger = Factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger(className);
    }

    private HttpClient CreateAuthenticatedJwtClient()
    {
        var http = Factory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GenerateToken(UserId.ToString())
        );
        AddTraceHeaders(http);

        return http;
    }

    private HttpClient CreateAuthenticatedApiKeyClient()
    {
        var http = Factory.CreateClient();
        http.DefaultRequestHeaders.Add("X-API-Key", "test-apikey");
        AddTraceHeaders(http);

        return http;
    }

    private static void AddTraceHeaders(HttpClient http)
    {
        if (Activity.Current != null)
        {
            var traceparent = $"00-{Activity.Current.TraceId.ToHexString()}-{Activity.Current.SpanId.ToHexString()}-01";
            http.DefaultRequestHeaders.Add("traceparent", traceparent);
            if (!string.IsNullOrEmpty(Activity.Current.TraceStateString))
                http.DefaultRequestHeaders.Add("tracestate", Activity.Current.TraceStateString);
        }
    }

    private static string GenerateToken(string sub)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"gen-dev-jwt.sh {sub}",
            WorkingDirectory = AppContext.BaseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var proc = Process.Start(psi);
        var jwtToken = proc!.StandardOutput.ReadToEnd();
        var errorOutput = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        if (proc.ExitCode != 0)
            throw new InvalidOperationException(
                $"Token generation script failed with exit code {proc.ExitCode}: {errorOutput}"
            );

        jwtToken = jwtToken.Trim();
        return jwtToken;
    }

    public BookmarkBuilder NewBookmarkBuilder() => new BookmarkBuilder().WithUserId(UserId.ToString());

    public TagBuilder NewTagBuilder() => new TagBuilder().WithUserId(UserId.ToString());

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Connection?.Dispose();
            _activity?.Dispose();
            Factory?.Dispose();
            HttpClientJwt?.Dispose();
            HttpClientApiKey?.Dispose();
        }
    }

    private static void SetEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        if (Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") == null)
            Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "bookmarks-api-tests");
        if (Environment.GetEnvironmentVariable("OTEL_SDK_DISABLED") == null)
            Environment.SetEnvironmentVariable("OTEL_SDK_DISABLED", "false");

        // return;

        // OTEL Exporter
        if (Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") == null)
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:5088");
        if (Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL") == null)
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");
        if (Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_TIMEOUT") == null)
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TIMEOUT", "10000");
        if (Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_COMPRESSION") == null)
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_COMPRESSION", "none");
        if (Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_INSECURE") == null)
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_INSECURE", "true");

        // General SDK Configuration
        if (Environment.GetEnvironmentVariable("OTEL_SDK_DISABLED") == null)
            Environment.SetEnvironmentVariable("OTEL_SDK_DISABLED", "false");
        if (Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES") == null)
            Environment.SetEnvironmentVariable(
                "OTEL_RESOURCE_ATTRIBUTES",
                "service.version=1.0,service.instance.id=test-instance,deployment.environment=production"
            );
        if (Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") == null)
            Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "bookmarks-api-tests");
        if (Environment.GetEnvironmentVariable("OTEL_TRACES_EXPORTER") == null)
            Environment.SetEnvironmentVariable("OTEL_TRACES_EXPORTER", "otlp");
        if (Environment.GetEnvironmentVariable("OTEL_METRICS_EXPORTER") == null)
            Environment.SetEnvironmentVariable("OTEL_METRICS_EXPORTER", "otlp");
        if (Environment.GetEnvironmentVariable("OTEL_LOGS_EXPORTER") == null)
            Environment.SetEnvironmentVariable("OTEL_LOGS_EXPORTER", "otlp");
        if (Environment.GetEnvironmentVariable("OTEL_TRACES_SAMPLER") == null)
            Environment.SetEnvironmentVariable("OTEL_TRACES_SAMPLER", "always_on");
        if (Environment.GetEnvironmentVariable("OTEL_TRACES_SAMPLER_ARG") == null)
            Environment.SetEnvironmentVariable("OTEL_TRACES_SAMPLER_ARG", "");
        if (Environment.GetEnvironmentVariable("OTEL_LOG_LEVEL") == null)
            Environment.SetEnvironmentVariable("OTEL_LOG_LEVEL", "warn");
        if (Environment.GetEnvironmentVariable("OTEL_PROPAGATORS") == null)
            Environment.SetEnvironmentVariable("OTEL_PROPAGATORS", "tracecontext,baggage");

        // Batch Span Processor
        if (Environment.GetEnvironmentVariable("OTEL_BSP_SCHEDULE_DELAY") == null)
            Environment.SetEnvironmentVariable("OTEL_BSP_SCHEDULE_DELAY", "2000");
        if (Environment.GetEnvironmentVariable("OTEL_BSP_EXPORT_TIMEOUT") == null)
            Environment.SetEnvironmentVariable("OTEL_BSP_EXPORT_TIMEOUT", "30000");
        if (Environment.GetEnvironmentVariable("OTEL_BSP_MAX_QUEUE_SIZE") == null)
            Environment.SetEnvironmentVariable("OTEL_BSP_MAX_QUEUE_SIZE", "2048");
        if (Environment.GetEnvironmentVariable("OTEL_BSP_MAX_EXPORT_BATCH_SIZE") == null)
            Environment.SetEnvironmentVariable("OTEL_BSP_MAX_EXPORT_BATCH_SIZE", "512");

        // Batch LogRecord Processor
        if (Environment.GetEnvironmentVariable("OTEL_BLRP_SCHEDULE_DELAY") == null)
            Environment.SetEnvironmentVariable("OTEL_BLRP_SCHEDULE_DELAY", "2000");
        if (Environment.GetEnvironmentVariable("OTEL_BLRP_EXPORT_TIMEOUT") == null)
            Environment.SetEnvironmentVariable("OTEL_BLRP_EXPORT_TIMEOUT", "30000");
        if (Environment.GetEnvironmentVariable("OTEL_BLRP_MAX_QUEUE_SIZE") == null)
            Environment.SetEnvironmentVariable("OTEL_BLRP_MAX_QUEUE_SIZE", "2048");
        if (Environment.GetEnvironmentVariable("OTEL_BLRP_MAX_EXPORT_BATCH_SIZE") == null)
            Environment.SetEnvironmentVariable("OTEL_BLRP_MAX_EXPORT_BATCH_SIZE", "512");

        // Periodic exporting MetricReader
        if (Environment.GetEnvironmentVariable("OTEL_METRIC_EXPORT_INTERVAL") == null)
            Environment.SetEnvironmentVariable("OTEL_METRIC_EXPORT_INTERVAL", "5000");
        if (Environment.GetEnvironmentVariable("OTEL_METRIC_EXPORT_TIMEOUT") == null)
            Environment.SetEnvironmentVariable("OTEL_METRIC_EXPORT_TIMEOUT", "3000");
    }
}
