using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class OtelCollectorServiceExtensions
{
    public static IResourceBuilder<ContainerResource> AddOtelCollector(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ContainerResource>? prometheus = null
    )
    {
        var otelCollector = builder
            .AddContainer(name: name, image: "otel/opentelemetry-collector-contrib", tag: "0.142.0")
            .WithEndpoint(
                "grpc",
                e =>
                {
                    e.TargetPort = 4317;
                    e.UriScheme = "http";
                }
            )
            .WithHttpEndpoint(name: "http", targetPort: 4318)
            .WithEndpoint(name: "fluentforward", targetPort: 8006, protocol: ProtocolType.Tcp)
            .WithOtlpExporter()
            .WithContainerFiles(
                "/etc/otelcol-contrib",
                async (ctx, ct) =>
                {
                    // When a Prometheus resource is provided, metrics are also pushed to it
                    // via the prometheusremotewrite exporter (in addition to the dashboard).
                    var (prometheusExporter, metricsExporters) = prometheus is null
                        ? ("", "[otlp, debug]")
                        : (
                            $"""
                              prometheusremotewrite:
                                endpoint: http://{await prometheus.GetEndpoint("http", KnownNetworkIdentifiers.DefaultAspireContainerNetwork).Property(
                                EndpointProperty.HostAndPort
                            ).GetValueAsync(ct)}
                            """,
                            "[otlp, debug, prometheusremotewrite]"
                        );

                    var config = $$"""
                        receivers:
                          otlp:
                            protocols:
                              grpc:
                                endpoint: 0.0.0.0:4317
                              http:
                                endpoint: 0.0.0.0:4318
                                cors:
                                  allowed_origins:
                                    - "*"
                                  allowed_headers:
                                    - "Content-Type"
                                  max_age: 7200
                          fluentforward:
                            endpoint: 0.0.0.0:8006

                        exporters:
                          otlp:
                            endpoint: ${OTEL_EXPORTER_OTLP_ENDPOINT}
                            tls:
                              insecure: true
                          debug:
                            verbosity: detailed
                        {{prometheusExporter}}

                        service:
                          pipelines:
                            traces:
                              receivers: [otlp]
                              exporters: [otlp, debug]
                            metrics:
                              receivers: [otlp]
                              exporters: {{metricsExporters}}
                            logs:
                              receivers: [otlp]
                              exporters: [otlp, debug]
                            logs/fluentd:
                              receivers: [fluentforward]
                              exporters: [otlp, debug]
                        """;

                    return [new ContainerFile { Name = "config.yaml", Contents = config }];
                }
            )
            .WithArgs("--config", "/etc/otelcol-contrib/config.yaml");

        return otelCollector;
    }

    /// <summary>
    /// Routes all telemetry (traces, metrics, logs) through the OTel collector instead of
    /// the Aspire dashboard directly. The collector forwards everything to the dashboard.
    /// </summary>
    public static IResourceBuilder<T> WithOtlpExporterViaCollector<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<ContainerResource> otelCollector
    )
        where T : IResourceWithEnvironment
    {
        // WithOtlpExporter registers an environment callback that sets the endpoint to the
        // dashboard. A callback added afterwards runs later and overrides it with the
        // collector's gRPC endpoint.
        return builder
            .WithOtlpExporter()
            .WithEnvironment(ctx =>
            {
                ctx.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = otelCollector.GetEndpoint("grpc");
            });
    }
}
