using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ServiceDefaults.OpenTelemetry.HttpClientBodyEnrichment;

namespace ServiceDefaults;

public static class OpenTelemetryExtensions
{
    public static TBuilder AddCustomOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        Monitoring.ActivitySource = new ActivitySource(builder.Environment.ApplicationName);
        var _ = new NameResolutionEventSourceListener(Monitoring.ActivitySource);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder
            .Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(builder.Environment.ApplicationName)
                    .AddMeter("Polly")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(tracing =>
                    {
                        tracing.RecordException = true;
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthCheckExtension.HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(HealthCheckExtension.AlivenessEndpointPath);

                        tracing.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.request_protocol", request.Protocol);
                        };
                        tracing.EnrichWithHttpResponse = (activity, response) =>
                        {
                            if (
                                response
                                    .HttpContext.Features.Get<IAuthenticateResultFeature>()
                                    ?.AuthenticateResult?.Ticket?.AuthenticationScheme
                                is string schemeName
                            )
                                activity.SetTag("asp.authtype", schemeName);

                            activity.SetTag("http.responsbooe_length", response.ContentLength);
                        };
                    })
                    .AddHttpClientInstrumentation(x =>
                    {
                        if (builder.Environment.IsDevelopment())
                        {
                            x.ConfigureHttpClientBodyEnrichment();
                            x.EnrichWithHttpRequestMessage = (act, msg) =>
                            {
                                // It redacts query params by default. Override for development
                                act.AddTag("url.full", msg.RequestUri?.ToString() ?? "unknown");
                            };
                        }
                    });
            });

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            builder.Services.AddOpenTelemetry().UseOtlpExporter();

        return builder;
    }
}

public class Monitoring
{
    public static ActivitySource ActivitySource { get; set; } = null!;
}

public class NameResolutionEventSourceListener(ActivitySource activitySource) : EventListener
{
    private const string ActivityName = "ResolveDns";

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "System.Net.NameResolution")
            EnableEvents(eventSource, EventLevel.Informational, EventKeywords.All);
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData.EventSource.Name != "System.Net.NameResolution")
            return;

        switch (eventData.EventName)
        {
            case "ResolutionStart":
                var activity = activitySource.StartActivity(ActivityName, kind: ActivityKind.Internal);
                if (activity is null)
                    return;
                for (int i = 0; i < eventData.Payload?.Count; i++)
                    activity.SetTag(eventData.PayloadNames![i], eventData.Payload[i]!.ToString());
                break;
            case "ResolutionStop" when Activity.Current?.DisplayName == ActivityName:
                Activity.Current.Dispose();
                break;
            case "ResolutionFailed" when Activity.Current?.DisplayName == ActivityName:
                Activity.Current.SetStatus(ActivityStatusCode.Error, "resolution failed");
                Activity.Current.Dispose();
                break;
        }
    }
}
