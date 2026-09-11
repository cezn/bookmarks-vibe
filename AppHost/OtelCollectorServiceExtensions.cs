using System.Net.Sockets;

namespace Aspire.Hosting;

public static class OtelCollectorServiceExtensions
{
    public static IResourceBuilder<ContainerResource> AddOtelCollector(
        this IDistributedApplicationBuilder builder,
        string name
    )
    {
        var otelCollector = builder
            .AddContainer(name: name, image: "otel/opentelemetry-collector-contrib", tag: "0.142.0")
            .WithEndpoint(name: "grpc", targetPort: 4317, protocol: ProtocolType.Tcp)
            .WithHttpEndpoint(name: "http", targetPort: 4318)
            .WithEndpoint(name: "fluentforward", targetPort: 8006, protocol: ProtocolType.Tcp)
            .WithOtlpExporter()
            .WithContainerFiles(
                "/etc/otelcol-contrib",
                [
                    new ContainerFile
                    {
                        Name = "config.yaml",
                        Contents = """
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

                        service:
                          pipelines:
                            traces:
                              receivers: [otlp]
                              exporters: [otlp, debug]
                            metrics:
                              receivers: [otlp]
                              exporters: [otlp, debug]
                            logs:
                              receivers: [otlp]
                              exporters: [otlp, debug]
                            logs/fluentd:
                              receivers: [fluentforward]
                              exporters: [otlp, debug]
                        """,
                    },
                ]
            )
            .WithArgs("--config", "/etc/otelcol-contrib/config.yaml");

        return otelCollector;
    }
}
